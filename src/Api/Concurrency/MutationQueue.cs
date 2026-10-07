using System.Threading.Channels;

namespace Api.Concurrency;

/// <summary>
/// Redis-style single-threaded mutation event loop.
/// Thread is pinned to a dedicated CPU core (affinity).
/// </summary>
public sealed class MutationQueue : IAsyncDisposable
{
    private readonly Channel<IMutationWork> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private long _sequence;
    private readonly int _pinnedCore;

    public MutationQueue(int capacity = 10_000, int? pinCore = null)
    {
        _pinnedCore = pinCore ?? CpuAffinity.PreferredMutationCore;

        _channel = Channel.CreateBounded<IMutationWork>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _loop = Task.Factory.StartNew(
            RunLoop,
            _cts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    public long CurrentFence => Interlocked.Read(ref _sequence);
    public int PinnedCore => _pinnedCore;

    public async Task<T> EnqueueAsync<T>(
        Func<long, CancellationToken, ValueTask<T>> work,
        CancellationToken ct = default)
    {
        var item = new MutationWork<T>(work);
        await _channel.Writer.WriteAsync(item, ct).ConfigureAwait(false);
        return await item.Completion.ConfigureAwait(false);
    }

    public async Task EnqueueAsync(
        Func<long, CancellationToken, ValueTask> work,
        CancellationToken ct = default)
    {
        await EnqueueAsync<object?>(async (fence, token) =>
        {
            await work(fence, token).ConfigureAwait(false);
            return null;
        }, ct).ConfigureAwait(false);
    }

    private async Task RunLoop()
    {
        var pinned = CpuAffinity.TryPinCurrentThread(_pinnedCore);
        CpuAffinity.LogPinResult(pinned, _pinnedCore, "MutationQueue");

        try { Thread.CurrentThread.Priority = ThreadPriority.AboveNormal; }
        catch { }

        var reader = _channel.Reader;
        var token = _cts.Token;

        await foreach (var work in reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            var fence = Interlocked.Increment(ref _sequence);
            try { await work.ExecuteAsync(fence, token).ConfigureAwait(false); }
            catch (Exception ex) { work.Fail(ex); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        try { await _loop.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        _cts.Dispose();
    }

    private interface IMutationWork
    {
        ValueTask ExecuteAsync(long fence, CancellationToken ct);
        void Fail(Exception ex);
    }

    private sealed class MutationWork<T> : IMutationWork
    {
        private readonly Func<long, CancellationToken, ValueTask<T>> _work;
        private readonly TaskCompletionSource<T> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MutationWork(Func<long, CancellationToken, ValueTask<T>> work) => _work = work;
        public Task<T> Completion => _tcs.Task;

        public async ValueTask ExecuteAsync(long fence, CancellationToken ct)
        {
            var result = await _work(fence, ct).ConfigureAwait(false);
            _tcs.TrySetResult(result);
        }

        public void Fail(Exception ex) => _tcs.TrySetException(ex);
    }
}
