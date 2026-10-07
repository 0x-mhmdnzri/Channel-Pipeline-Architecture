namespace Api.Concurrency;

public sealed class WriteGate
{
    private readonly MutationQueue _queue;
    private readonly IdempotencyStore _store;

    public WriteGate(MutationQueue queue, IdempotencyStore store)
    {
        _queue = queue;
        _store = store;
    }

    public long CurrentFence => _queue.CurrentFence;

    public async Task<WriteResult<T>> ExecuteAsync<T>(
        string idempotencyKey,
        long? clientFence,
        string? fenceResourceKey,
        Func<long, CancellationToken, ValueTask<T>> mutation,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key is required for writes.", nameof(idempotencyKey));

        if (_store.TryGet(idempotencyKey, out var existing))
        {
            return new WriteResult<T>(
                Replay: true,
                FencingToken: existing.FencingToken,
                Value: existing.Result is T t ? t : default!);
        }

        return await _queue.EnqueueAsync(async (fence, token) =>
        {
            if (_store.TryGet(idempotencyKey, out var raced))
            {
                return new WriteResult<T>(
                    Replay: true,
                    FencingToken: raced.FencingToken,
                    Value: raced.Result is T t ? t : default!);
            }

            if (clientFence is not null && fenceResourceKey is not null)
            {
                if (!_store.ValidateFence(fenceResourceKey, clientFence.Value, out var current))
                    throw new FencingConflictException(fenceResourceKey, clientFence.Value, current);
            }

            var value = await mutation(fence, token).ConfigureAwait(false);
            _store.TryCommit(idempotencyKey, fence, value);

            if (fenceResourceKey is not null)
                _store.SetFence(fenceResourceKey, fence);

            return new WriteResult<T>(Replay: false, FencingToken: fence, Value: value);
        }, ct).ConfigureAwait(false);
    }
}

public sealed record WriteResult<T>(bool Replay, long FencingToken, T Value);

public sealed class FencingConflictException : Exception
{
    public string ResourceKey { get; }
    public long ClientFence { get; }
    public long CurrentFence { get; }

    public FencingConflictException(string resourceKey, long clientFence, long currentFence)
        : base($"Fencing conflict on '{resourceKey}': client sent {clientFence}, current is {currentFence}.")
    {
        ResourceKey = resourceKey;
        ClientFence = clientFence;
        CurrentFence = currentFence;
    }
}
