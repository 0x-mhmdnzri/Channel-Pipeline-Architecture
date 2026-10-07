using System.Threading.Channels;
using Models;

namespace Pipelines.Stages;

public sealed class EnrichStage : IPipelineStage
{
    private readonly ChannelReader<TransformedRecord> _input;
    private readonly ChannelWriter<EnrichedRecord> _output;
    private readonly int _degreeOfParallelism;

    public EnrichStage(
        ChannelReader<TransformedRecord> input,
        ChannelWriter<EnrichedRecord> output,
        int degreeOfParallelism = 2)
    {
        _input = input;
        _output = output;
        _degreeOfParallelism = degreeOfParallelism;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var tasks = Enumerable
            .Range(0, _degreeOfParallelism)
            .Select(_ => ProcessAsync(cancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
        _output.Complete();
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await foreach (var item in _input.ReadAllAsync(ct).ConfigureAwait(false))
        {
            var enriched = Enrich(item);
            await _output.WriteAsync(enriched, ct).ConfigureAwait(false);
        }
    }

    private static EnrichedRecord Enrich(TransformedRecord item)
    {
        // Simulate enrichment (lookup, external call, etc.)
        var extra = $"enriched-{item.Id}";
        return new EnrichedRecord(item.Id, item.NormalizedPayload, extra, item.ProcessedAt);
    }
}
