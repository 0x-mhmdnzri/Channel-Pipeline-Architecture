using System.Text.Json;
using Api.Concurrency;
using Api.Data;
using Api.Models;
using Npgsql;

namespace Api.Endpoints;

/// <summary>
/// Ultra-low-latency endpoints for multi-million req/min targets.
/// /api/bench/* never touches Postgres on the hot path.
/// </summary>
public static class BenchEndpoints
{
    private static readonly byte[] PingJson = "{""ok"":true,""svc"":""bench""}"u8.ToArray();

    public static RouteGroupBuilder MapBenchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bench").WithTags("Bench");

        group.MapGet("/ping", () => Results.Bytes(PingJson, "application/json"))
            .WithName("BenchPing");

        group.MapGet("/employees", (HotCache cache) =>
        {
            var payload = cache.GetEmployeesJson();
            return Results.Bytes(payload, "application/json");
        })
        .WithName("BenchEmployees");

        group.MapPost("/warm", async (HotCache cache, CancellationToken ct) =>
        {
            await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand("""
                SELECT id, department_id, first_name, last_name, email, title, salary, hired_at
                FROM employees ORDER BY id LIMIT 50
                """, conn);

            var list = new List<Employee>(50);
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

            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            cache.SetEmployees(list, opts);

            return Results.Ok(new WarmResult(cache.EmployeesCount, cache.PayloadBytes, cache.Version));
        })
        .WithName("BenchWarm");

        group.MapGet("/stats", (HotCache cache, MutationQueue queue) =>
            Results.Ok(new BenchStats(
                cache.Hits, cache.EmployeesCount, cache.PayloadBytes, cache.Version,
                queue.CurrentFence, queue.PinnedCore, Environment.ProcessorCount)))
        .WithName("BenchStats");

        return group;
    }
}

public sealed record WarmResult(int Count, int PayloadBytes, long Version);
public sealed record BenchStats(
    long CacheHits, int EmployeesCached, int PayloadBytes, long CacheVersion,
    long Fence, int PinnedCore, int ProcessorCount);
