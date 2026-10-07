using Api.Data;
using Api.Models;
using Npgsql;

namespace Api.Endpoints;

public static class OrganizationsEndpoints
{
    public static RouteGroupBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations")
            .WithTags("Organizations");

        group.MapGet("/", async (CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(
                "SELECT id, name, country, created_at FROM organizations ORDER BY id", conn);

            var list = new List<Organization>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(new Organization(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3)));
            }
            return Results.Ok(list);
        })
        .WithName("ListOrganizations")
        .WithSummary("List all organizations");

        group.MapGet("/{id:int}", async (int id, CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

            await using var orgCmd = new NpgsqlCommand(
                "SELECT id, name, country FROM organizations WHERE id = @id", conn);
            orgCmd.Parameters.AddWithValue("id", id);

            await using var orgReader = await orgCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await orgReader.ReadAsync(ct).ConfigureAwait(false))
                return Results.NotFound();

            var orgId = orgReader.GetInt32(0);
            var orgName = orgReader.GetString(1);
            var country = orgReader.GetString(2);
            await orgReader.CloseAsync().ConfigureAwait(false);

            await using var cCmd = new NpgsqlCommand("""
                SELECT c.id, c.name, c.industry,
                       (SELECT COUNT(*) FROM departments d WHERE d.company_id = c.id),
                       (SELECT COUNT(*) FROM employees e
                        JOIN departments d ON d.id = e.department_id
                        WHERE d.company_id = c.id)
                FROM companies c
                WHERE c.organization_id = @id
                ORDER BY c.id
                """, conn);
            cCmd.Parameters.AddWithValue("id", id);

            var companies = new List<CompanySummary>();
            await using var cReader = await cCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await cReader.ReadAsync(ct).ConfigureAwait(false))
            {
                companies.Add(new CompanySummary(
                    cReader.GetInt32(0),
                    cReader.GetString(1),
                    cReader.GetString(2),
                    (int)cReader.GetInt64(3),
                    (int)cReader.GetInt64(4)));
            }

            return Results.Ok(new OrganizationDetail(orgId, orgName, country, companies));
        })
        .WithName("GetOrganization")
        .WithSummary("Get organization with nested companies summary");

        return group;
    }
}
