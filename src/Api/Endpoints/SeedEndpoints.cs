using Api.Concurrency;
using Api.Data;
using Api.Models;

namespace Api.Endpoints;

public static class SeedEndpoints
{
    public static RouteGroupBuilder MapSeedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/seed")
            .WithTags("Seed");

        group.MapPost("/", async (
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

            try
            {
                var result = await gate.ExecuteAsync(
                    idempotencyKey: idemKey,
                    clientFence: clientFence,
                    fenceResourceKey: "seed",
                    mutation: async (_, token) =>
                    {
                        return await Seeder.SeedAsync(
                            organizationCount: 20,
                            companiesPerOrg: 3,
                            departmentsPerCompany: 4,
                            employeesPerDept: 15,
                            projectsPerDept: 3,
                            tasksPerProject: 8,
                            token);
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
        })
        .WithName("SeedDatabase")
        .WithSummary("Seed DB via single-threaded mutation loop (Idempotency-Key required)");

        group.MapPost("/large", async (
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

            try
            {
                var result = await gate.ExecuteAsync(
                    idempotencyKey: idemKey,
                    clientFence: clientFence,
                    fenceResourceKey: "seed",
                    mutation: async (_, token) =>
                    {
                        return await Seeder.SeedAsync(
                            organizationCount: 50,
                            companiesPerOrg: 4,
                            departmentsPerCompany: 5,
                            employeesPerDept: 25,
                            projectsPerDept: 4,
                            tasksPerProject: 10,
                            token);
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
        })
        .WithName("SeedDatabaseLarge")
        .WithSummary("Large seed via single-threaded mutation loop");

        return group;
    }
}
