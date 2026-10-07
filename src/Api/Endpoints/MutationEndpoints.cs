using Api.Concurrency;
using Api.Data;
using Npgsql;

namespace Api.Endpoints;

public static class MutationEndpoints
{
    public static RouteGroupBuilder MapMutationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mutations")
            .WithTags("Mutations");

        group.MapGet("/fence", (WriteGate gate) =>
            Results.Ok(new { fencingToken = gate.CurrentFence }))
            .WithName("GetCurrentFence")
            .WithSummary("Current global fencing token from the mutation event loop");

        group.MapPost("/employee-title", async (
            UpdateTitleRequest body,
            HttpRequest request,
            WriteGate gate,
            CancellationToken ct) =>
        {
            var idemKey = request.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(idemKey))
                return Results.BadRequest(new { error = "Idempotency-Key header is required." });

            long? clientFence = null;
            if (long.TryParse(request.Headers["X-Fencing-Token"].FirstOrDefault(), out var f))
                clientFence = f;

            var resourceKey = $"employee:{body.EmployeeId}";

            try
            {
                var result = await gate.ExecuteAsync(
                    idempotencyKey: idemKey,
                    clientFence: clientFence,
                    fenceResourceKey: resourceKey,
                    mutation: async (fence, token) =>
                    {
                        await using var conn = await Db.OpenAsync(token).ConfigureAwait(false);
                        await using var cmd = new NpgsqlCommand(
                            "UPDATE employees SET title = @t WHERE id = @id RETURNING id, title",
                            conn);
                        cmd.Parameters.AddWithValue("t", body.Title);
                        cmd.Parameters.AddWithValue("id", body.EmployeeId);

                        await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
                        if (!await reader.ReadAsync(token).ConfigureAwait(false))
                            throw new KeyNotFoundException($"Employee {body.EmployeeId} not found.");

                        return new UpdateTitleResponse(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            fence);
                    },
                    ct);

                return Results.Json(new
                {
                    replay = result.Replay,
                    fencingToken = result.FencingToken,
                    data = result.Value
                });
            }
            catch (FencingConflictException ex)
            {
                return Results.Conflict(new
                {
                    error = "fencing_conflict",
                    resource = ex.ResourceKey,
                    clientFence = ex.ClientFence,
                    currentFence = ex.CurrentFence
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("UpdateEmployeeTitle")
        .WithSummary("Serial write via event loop — Idempotency-Key + optional fencing token");

        return group;
    }
}

public sealed record UpdateTitleRequest(int EmployeeId, string Title);
public sealed record UpdateTitleResponse(int EmployeeId, string Title, long Fence);
