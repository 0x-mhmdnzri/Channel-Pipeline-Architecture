using System.Diagnostics;
using Api.Models;
using Npgsql;

namespace Api.Data;

public static class Seeder
{
    private static readonly string[] Countries = ["Iran", "Germany", "USA", "Canada", "Netherlands", "Sweden", "Japan", "Australia"];
    private static readonly string[] Industries = ["FinTech", "HealthTech", "E-Commerce", "Logistics", "AI/ML", "SaaS", "CyberSecurity", "Energy"];
    private static readonly string[] DeptNames = ["Engineering", "Product", "Sales", "Marketing", "HR", "Finance", "Support", "Research"];
    private static readonly string[] Titles = ["Software Engineer", "Senior Engineer", "Tech Lead", "Product Manager", "Designer", "Analyst", "Director", "Specialist"];
    private static readonly string[] Cities = ["Tehran", "Berlin", "New York", "Toronto", "Amsterdam", "Stockholm", "Tokyo", "Sydney"];
    private static readonly string[] ProjectStatuses = ["Planning", "Active", "OnHold", "Completed"];
    private static readonly string[] TaskStatuses = ["Todo", "InProgress", "Review", "Done"];

    public static async Task<SeedResult> SeedAsync(
        int organizationCount = 20,
        int companiesPerOrg = 3,
        int departmentsPerCompany = 4,
        int employeesPerDept = 15,
        int projectsPerDept = 3,
        int tasksPerProject = 8,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        await Schema.DropAllAsync(ct).ConfigureAwait(false);
        await Schema.EnsureCreatedAsync(ct).ConfigureAwait(false);

        await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

        var rnd = new Random(42);
        int orgCount = 0, companyCount = 0, deptCount = 0, empCount = 0, addrCount = 0, projCount = 0, taskCount = 0;

        var orgIds = new List<int>(organizationCount);
        for (int i = 0; i < organizationCount; i++)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO organizations (name, country) VALUES (@n, @c) RETURNING id", conn, tx);
            cmd.Parameters.AddWithValue("n", $"Org-{i + 1:D3}");
            cmd.Parameters.AddWithValue("c", Countries[rnd.Next(Countries.Length)]);
            orgIds.Add((int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!);
            orgCount++;
        }

        var companyIds = new List<int>();
        foreach (var orgId in orgIds)
        {
            for (int c = 0; c < companiesPerOrg; c++)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO companies (organization_id, name, industry, founded_at) VALUES (@o, @n, @i, @f) RETURNING id",
                    conn, tx);
                cmd.Parameters.AddWithValue("o", orgId);
                cmd.Parameters.AddWithValue("n", $"Company-{orgId}-{c + 1}");
                cmd.Parameters.AddWithValue("i", Industries[rnd.Next(Industries.Length)]);
                cmd.Parameters.AddWithValue("f", DateTime.UtcNow.AddYears(-rnd.Next(1, 20)));
                companyIds.Add((int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!);
                companyCount++;
            }
        }

        var deptIds = new List<int>();
        foreach (var companyId in companyIds)
        {
            for (int d = 0; d < departmentsPerCompany; d++)
            {
                var name = DeptNames[d % DeptNames.Length];
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO departments (company_id, name, code, budget) VALUES (@c, @n, @code, @b) RETURNING id",
                    conn, tx);
                cmd.Parameters.AddWithValue("c", companyId);
                cmd.Parameters.AddWithValue("n", name);
                cmd.Parameters.AddWithValue("code", $"{name[..3].ToUpperInvariant()}-{companyId}");
                cmd.Parameters.AddWithValue("b", rnd.Next(50_000, 2_000_000));
                deptIds.Add((int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!);
                deptCount++;
            }
        }

        var empIdsByDept = new Dictionary<int, List<int>>();
        int empSeq = 0;
        foreach (var deptId in deptIds)
        {
            var list = new List<int>(employeesPerDept);
            for (int e = 0; e < employeesPerDept; e++)
            {
                empSeq++;
                var first = $"First{empSeq}";
                var last = $"Last{empSeq}";
                var email = $"user{empSeq}@example.com";

                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO employees (department_id, first_name, last_name, email, title, salary, hired_at)
                    VALUES (@d, @f, @l, @e, @t, @s, @h) RETURNING id
                    """, conn, tx);
                cmd.Parameters.AddWithValue("d", deptId);
                cmd.Parameters.AddWithValue("f", first);
                cmd.Parameters.AddWithValue("l", last);
                cmd.Parameters.AddWithValue("e", email);
                cmd.Parameters.AddWithValue("t", Titles[rnd.Next(Titles.Length)]);
                cmd.Parameters.AddWithValue("s", rnd.Next(40_000, 180_000));
                cmd.Parameters.AddWithValue("h", DateTime.UtcNow.AddDays(-rnd.Next(30, 3000)));
                var empId = (int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
                list.Add(empId);
                empCount++;

                int addrCountForEmp = rnd.Next(1, 3);
                for (int a = 0; a < addrCountForEmp; a++)
                {
                    await using var acmd = new NpgsqlCommand(
                        """
                        INSERT INTO addresses (employee_id, street, city, country, postal_code, is_primary)
                        VALUES (@e, @s, @c, @co, @p, @pr)
                        """, conn, tx);
                    acmd.Parameters.AddWithValue("e", empId);
                    acmd.Parameters.AddWithValue("s", $"{rnd.Next(1, 200)} Main St");
                    acmd.Parameters.AddWithValue("c", Cities[rnd.Next(Cities.Length)]);
                    acmd.Parameters.AddWithValue("co", Countries[rnd.Next(Countries.Length)]);
                    acmd.Parameters.AddWithValue("p", $"{rnd.Next(10000, 99999)}");
                    acmd.Parameters.AddWithValue("pr", a == 0);
                    await acmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    addrCount++;
                }
            }
            empIdsByDept[deptId] = list;
        }

        foreach (var deptId in deptIds)
        {
            var emps = empIdsByDept[deptId];
            for (int p = 0; p < projectsPerDept; p++)
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO projects (department_id, name, status, start_date, end_date)
                    VALUES (@d, @n, @s, @sd, @ed) RETURNING id
                    """, conn, tx);
                cmd.Parameters.AddWithValue("d", deptId);
                cmd.Parameters.AddWithValue("n", $"Project-{deptId}-{p + 1}");
                cmd.Parameters.AddWithValue("s", ProjectStatuses[rnd.Next(ProjectStatuses.Length)]);
                var start = DateTime.UtcNow.AddMonths(-rnd.Next(1, 24));
                cmd.Parameters.AddWithValue("sd", start);
                cmd.Parameters.AddWithValue("ed", rnd.Next(0, 2) == 0 ? (object)start.AddMonths(rnd.Next(3, 18)) : DBNull.Value);
                var projectId = (int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
                projCount++;

                for (int t = 0; t < tasksPerProject; t++)
                {
                    await using var tcmd = new NpgsqlCommand(
                        """
                        INSERT INTO tasks (project_id, assignee_id, title, status, priority)
                        VALUES (@p, @a, @t, @s, @pr)
                        """, conn, tx);
                    tcmd.Parameters.AddWithValue("p", projectId);
                    tcmd.Parameters.AddWithValue("a", emps.Count > 0 ? emps[rnd.Next(emps.Count)] : (object)DBNull.Value);
                    tcmd.Parameters.AddWithValue("t", $"Task-{projectId}-{t + 1}");
                    tcmd.Parameters.AddWithValue("s", TaskStatuses[rnd.Next(TaskStatuses.Length)]);
                    tcmd.Parameters.AddWithValue("pr", rnd.Next(1, 6));
                    await tcmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    taskCount++;
                }
            }
        }

        await tx.CommitAsync(ct).ConfigureAwait(false);
        sw.Stop();

        return new SeedResult(
            orgCount, companyCount, deptCount, empCount,
            addrCount, projCount, taskCount,
            sw.Elapsed.TotalSeconds);
    }
}
