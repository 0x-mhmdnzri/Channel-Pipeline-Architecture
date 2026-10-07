using Api.Data;
using Api.Models;
using Npgsql;

namespace Api.Endpoints;

public static class CompaniesEndpoints
{
    public static RouteGroupBuilder MapCompaniesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .WithTags("Companies");

        group.MapGet("/", async (int? organizationId, CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

            var sql = organizationId is null
                ? "SELECT id, organization_id, name, industry, founded_at FROM companies ORDER BY id LIMIT 200"
                : "SELECT id, organization_id, name, industry, founded_at FROM companies WHERE organization_id = @oid ORDER BY id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            if (organizationId is not null)
                cmd.Parameters.AddWithValue("oid", organizationId.Value);

            var list = new List<Company>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(new Company(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDateTime(4)));
            }
            return Results.Ok(list);
        })
        .WithName("ListCompanies")
        .WithSummary("List companies (optional filter by organizationId)");

        group.MapGet("/{id:int}", async (int id, CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = new NpgsqlCommand("""
                SELECT c.id, c.name, c.industry, o.id, o.name
                FROM companies c
                JOIN organizations o ON o.id = c.organization_id
                WHERE c.id = @id
                """, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return Results.NotFound();

            var companyId = reader.GetInt32(0);
            var name = reader.GetString(1);
            var industry = reader.GetString(2);
            var org = new OrganizationBrief(reader.GetInt32(3), reader.GetString(4));
            await reader.CloseAsync().ConfigureAwait(false);

            await using var dCmd = new NpgsqlCommand("""
                SELECT d.id, d.name, d.code,
                       (SELECT COUNT(*) FROM employees e WHERE e.department_id = d.id),
                       (SELECT COUNT(*) FROM projects p WHERE p.department_id = d.id)
                FROM departments d
                WHERE d.company_id = @id
                ORDER BY d.id
                """, conn);
            dCmd.Parameters.AddWithValue("id", id);

            var depts = new List<DepartmentSummary>();
            await using var dReader = await dCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await dReader.ReadAsync(ct).ConfigureAwait(false))
            {
                depts.Add(new DepartmentSummary(
                    dReader.GetInt32(0),
                    dReader.GetString(1),
                    dReader.GetString(2),
                    (int)dReader.GetInt64(3),
                    (int)dReader.GetInt64(4)));
            }

            return Results.Ok(new CompanyDetail(companyId, name, industry, org, depts));
        })
        .WithName("GetCompany")
        .WithSummary("Get company with nested departments summary");

        return group;
    }
}
