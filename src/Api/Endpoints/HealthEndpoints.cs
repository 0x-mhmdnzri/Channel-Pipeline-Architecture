using Api.Data;
using Npgsql;

namespace Api.Endpoints;

public sealed record HealthStatus(string Status, DateTimeOffset Time);
public sealed record DbHealthStatus(string Status, string Database);

public static class HealthEndpoints
{
    private static readonly byte[] OkJson = """{"status":"ok"}"""u8.ToArray();

    public static RouteGroupBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health").WithTags("Health");

        group.MapGet("/", () => Results.Bytes(OkJson, "application/json"))
            .WithName("HealthCheck")
            .WithSummary("Liveness probe (static bytes, zero-alloc)");

        group.MapGet("/db", async (CancellationToken ct) =>
        {
            try
            {
                await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                return Results.Ok(new DbHealthStatus("ok", "connected"));
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 503);
            }
        })
        .WithName("DbHealthCheck");

        return group;
    }
}
