using Api.Data;
using Api.Models;

namespace Api.Endpoints;

public static class SeedEndpoints
{
    public static RouteGroupBuilder MapSeedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/seed")
            .WithTags("Seed");

        group.MapPost("/", async (CancellationToken ct) =>
        {
            var result = await Seeder.SeedAsync(
                organizationCount: 20,
                companiesPerOrg: 3,
                departmentsPerCompany: 4,
                employeesPerDept: 15,
                projectsPerDept: 3,
                tasksPerProject: 8,
                ct);

            return Results.Ok(result);
        })
        .WithName("SeedDatabase")
        .WithSummary("Drop & re-seed the database with nested mock data");

        group.MapPost("/large", async (CancellationToken ct) =>
        {
            var result = await Seeder.SeedAsync(
                organizationCount: 50,
                companiesPerOrg: 4,
                departmentsPerCompany: 5,
                employeesPerDept: 25,
                projectsPerDept: 4,
                tasksPerProject: 10,
                ct);

            return Results.Ok(result);
        })
        .WithName("SeedDatabaseLarge")
        .WithSummary("Larger seed (~25k employees)");

        return group;
    }
}
