using System.Threading.Channels;
using Models;

namespace Pipelines.Stages;

public sealed class TransformStage : IPipelineStage
{
    private readonly ChannelReader<RawRecord> _input;
    private readonly ChannelWriter<TransformedRecord> _output;
    private readonly int _degreeOfParallelism;

    public TransformStage(
        ChannelReader<RawRecord> input,
        ChannelWriter<TransformedRecord> output,
        int degreeOfParallelism = 4)
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
            var transformed = Transform(item);
            await _output.WriteAsync(transformed, ct).ConfigureAwait(false);
        }
    }

    private static TransformedRecord Transform(RawRecord raw)
    {
        // Hot path — keep it allocation-light
        var normalized = raw.Payload.Trim().ToUpperInvariant();
        return new TransformedRecord(raw.Id, normalized, DateTimeOffset.UtcNow);
    }
}
