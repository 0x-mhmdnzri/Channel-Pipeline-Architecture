using Api.Data;
using Api.Models;
using Npgsql;

namespace Api.Endpoints;

public static class DepartmentsEndpoints
{
    public static RouteGroupBuilder MapDepartmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/departments")
            .WithTags("Departments");

        group.MapGet("/{id:int}", async (int id, CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = new NpgsqlCommand("""
                SELECT d.id, d.name, d.code, d.budget, c.id, c.name
                FROM departments d
                JOIN companies c ON c.id = d.company_id
                WHERE d.id = @id
                """, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return Results.NotFound();

            var deptId = reader.GetInt32(0);
            var name = reader.GetString(1);
            var code = reader.GetString(2);
            var budget = reader.GetInt32(3);
            var company = new CompanyBrief(reader.GetInt32(4), reader.GetString(5));
            await reader.CloseAsync().ConfigureAwait(false);

            await using var eCmd = new NpgsqlCommand("""
                SELECT id, first_name, last_name, email, title
                FROM employees WHERE department_id = @id ORDER BY id
                """, conn);
            eCmd.Parameters.AddWithValue("id", id);

            var employees = new List<EmployeeSummary>();
            await using var eReader = await eCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await eReader.ReadAsync(ct).ConfigureAwait(false))
            {
                employees.Add(new EmployeeSummary(
                    eReader.GetInt32(0),
                    $"{eReader.GetString(1)} {eReader.GetString(2)}",
                    eReader.GetString(3),
                    eReader.GetString(4)));
            }
            await eReader.CloseAsync().ConfigureAwait(false);

            await using var pCmd = new NpgsqlCommand("""
                SELECT p.id, p.name, p.status,
                       (SELECT COUNT(*) FROM tasks t WHERE t.project_id = p.id)
                FROM projects p WHERE p.department_id = @id ORDER BY p.id
                """, conn);
            pCmd.Parameters.AddWithValue("id", id);

            var projects = new List<ProjectSummary>();
            await using var pReader = await pCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await pReader.ReadAsync(ct).ConfigureAwait(false))
            {
                projects.Add(new ProjectSummary(
                    pReader.GetInt32(0),
                    pReader.GetString(1),
                    pReader.GetString(2),
                    (int)pReader.GetInt64(3)));
            }

            return Results.Ok(new DepartmentDetail(
                deptId, name, code, budget, company, employees, projects));
        })
        .WithName("GetDepartment")
        .WithSummary("Get department with nested employees and projects");

        return group;
    }
}
