using System.Collections.Concurrent;

namespace Api.Concurrency;

public sealed class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new(StringComparer.Ordinal);
    private readonly TimeSpan _ttl;

    public IdempotencyStore(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromHours(24);
    }

    public bool TryGet(string key, out IdempotencyEntry entry)
    {
        if (_entries.TryGetValue(key, out entry!))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
                return true;
            _entries.TryRemove(key, out _);
        }
        entry = default!;
        return false;
    }

    public bool TryCommit(string key, long fencingToken, object? result = null)
    {
        var entry = new IdempotencyEntry(key, fencingToken, result, DateTimeOffset.UtcNow.Add(_ttl));
        return _entries.TryAdd(key, entry);
    }

    public bool ValidateFence(string resourceKey, long clientFence, out long currentFence)
    {
        if (_entries.TryGetValue($"fence:{resourceKey}", out var entry) &&
            entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            currentFence = entry.FencingToken;
            return clientFence == currentFence;
        }
        currentFence = 0;
        return clientFence == 0;
    }

    public void SetFence(string resourceKey, long fencingToken)
    {
        var entry = new IdempotencyEntry($"fence:{resourceKey}", fencingToken, null, DateTimeOffset.UtcNow.Add(_ttl));
        _entries.AddOrUpdate($"fence:{resourceKey}", entry, (_, _) => entry);
    }

    public int Count => _entries.Count;
}

public sealed record IdempotencyEntry(
    string Key,
    long FencingToken,
    object? Result,
    DateTimeOffset ExpiresAt);
