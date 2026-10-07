using Api.Data;
using Api.Models;
using Npgsql;

namespace Api.Endpoints;

public static class EmployeesEndpoints
{
    public static RouteGroupBuilder MapEmployeesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees")
            .WithTags("Employees");

        group.MapGet("/", async (int? departmentId, int skip = 0, int take = 50, CancellationToken ct = default) =>
        {
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);

            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

            var sql = departmentId is null
                ? """
                  SELECT id, department_id, first_name, last_name, email, title, salary, hired_at
                  FROM employees ORDER BY id OFFSET @skip LIMIT @take
                  """
                : """
                  SELECT id, department_id, first_name, last_name, email, title, salary, hired_at
                  FROM employees WHERE department_id = @did ORDER BY id OFFSET @skip LIMIT @take
                  """;

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("skip", skip);
            cmd.Parameters.AddWithValue("take", take);
            if (departmentId is not null)
                cmd.Parameters.AddWithValue("did", departmentId.Value);

            var list = new List<Employee>();
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                list.Add(new Employee(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetDecimal(6),
                    reader.GetDateTime(7)));
            }
            return Results.Ok(list);
        })
        .WithName("ListEmployees")
        .WithSummary("List employees with optional department filter + paging");

        group.MapGet("/{id:int}", async (int id, CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = new NpgsqlCommand("""
                SELECT e.id, e.first_name, e.last_name, e.email, e.title, e.salary, e.hired_at,
                       d.id, d.name, d.code
                FROM employees e
                JOIN departments d ON d.id = e.department_id
                WHERE e.id = @id
                """, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                return Results.NotFound();

            var empId = reader.GetInt32(0);
            var first = reader.GetString(1);
            var last = reader.GetString(2);
            var email = reader.GetString(3);
            var title = reader.GetString(4);
            var salary = reader.GetDecimal(5);
            var hired = reader.GetDateTime(6);
            var dept = new DepartmentBrief(reader.GetInt32(7), reader.GetString(8), reader.GetString(9));
            await reader.CloseAsync().ConfigureAwait(false);

            await using var aCmd = new NpgsqlCommand("""
                SELECT id, employee_id, street, city, country, postal_code, is_primary
                FROM addresses WHERE employee_id = @id ORDER BY is_primary DESC, id
                """, conn);
            aCmd.Parameters.AddWithValue("id", id);

            var addresses = new List<Address>();
            await using var aReader = await aCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await aReader.ReadAsync(ct).ConfigureAwait(false))
            {
                addresses.Add(new Address(
                    aReader.GetInt32(0),
                    aReader.GetInt32(1),
                    aReader.GetString(2),
                    aReader.GetString(3),
                    aReader.GetString(4),
                    aReader.GetString(5),
                    aReader.GetBoolean(6)));
            }

            return Results.Ok(new EmployeeDetail(
                empId, first, last, email, title, salary, hired, dept, addresses));
        })
        .WithName("GetEmployee")
        .WithSummary("Get employee with nested addresses and department");

        return group;
    }
}
