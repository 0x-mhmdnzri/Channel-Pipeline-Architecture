using System.Threading.Channels;
using Models;

namespace Pipelines.Stages;

public sealed class PersistStage : IPipelineStage
{
    private readonly ChannelReader<EnrichedRecord> _input;
    private readonly int _degreeOfParallelism;

    public PersistStage(ChannelReader<EnrichedRecord> input, int degreeOfParallelism = 2)
    {
        _input = input;
        _degreeOfParallelism = degreeOfParallelism;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var tasks = Enumerable
            .Range(0, _degreeOfParallelism)
            .Select(_ => ProcessAsync(cancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        // Simple batching example
        var batch = new List<EnrichedRecord>(500);

        await foreach (var item in _input.ReadAllAsync(ct).ConfigureAwait(false))
        {
            batch.Add(item);

            if (batch.Count >= 500)
            {
                await PersistBatchAsync(batch, ct).ConfigureAwait(false);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await PersistBatchAsync(batch, ct).ConfigureAwait(false);
        }
    }

    private static Task PersistBatchAsync(List<EnrichedRecord> batch, CancellationToken ct)
    {
        // Replace with real DB / file / queue write
        // For demo we just count
        Console.WriteLine($"[Persist] Wrote batch of {batch.Count} records");
        return Task.CompletedTask;
    }
}
