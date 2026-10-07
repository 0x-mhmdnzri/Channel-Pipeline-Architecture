using System.Text.Json;
using Api.Models;

namespace Api.Concurrency;

/// <summary>
/// Pre-serialized response cache for multi-million req/min read path.
/// Serves immutable UTF-8 byte buffers — zero DB, zero JSON serialize on hot path.
/// </summary>
public sealed class HotCache
{
    private byte[] _employeesJson = "[]"u8.ToArray();
    private int _employeesCount;
    private long _hits;
    private long _version;

    public int EmployeesCount => _employeesCount;
    public long Hits => Interlocked.Read(ref _hits);
    public long Version => Interlocked.Read(ref _version);
    public int PayloadBytes => _employeesJson.Length;

    public void SetEmployees(List<Employee> employees, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(employees, options);
        Volatile.Write(ref _employeesJson, json);
        _employeesCount = employees.Count;
        Interlocked.Increment(ref _version);
    }

    public void SetEmployeesRaw(byte[] utf8Json, int count)
    {
        Volatile.Write(ref _employeesJson, utf8Json);
        _employeesCount = count;
        Interlocked.Increment(ref _version);
    }

    public ReadOnlyMemory<byte> GetEmployeesJson()
    {
        Interlocked.Increment(ref _hits);
        return Volatile.Read(ref _employeesJson);
    }
}
