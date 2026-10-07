using System.Threading.Channels;
using Models;
using Pipelines.Stages;

namespace Pipelines;

public sealed class PipelineBuilder
{
    private ChannelReader<RawRecord>? _ingestionReader;
    private readonly List<IPipelineStage> _stages = new();

    public PipelineBuilder WithIngestion(ChannelReader<RawRecord> reader)
    {
        _ingestionReader = reader;
        return this;
    }

    public PipelineBuilder AddTransformStage(int workers = 4, int capacity = 1000)
    {
        if (_ingestionReader is null)
            throw new InvalidOperationException("Call WithIngestion first.");

        var output = Channel.CreateBounded<TransformedRecord>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });

        var stage = new TransformStage(_ingestionReader, output.Writer, workers);
        _stages.Add(stage);

        // Keep the writer for the next stage
        _lastTransformedWriter = output.Writer;
        _lastTransformedReader = output.Reader;
        return this;
    }

    private ChannelWriter<TransformedRecord>? _lastTransformedWriter;
    private ChannelReader<TransformedRecord>? _lastTransformedReader;

    public PipelineBuilder AddEnrichStage(int workers = 2, int capacity = 1000)
    {
        if (_lastTransformedReader is null)
            throw new InvalidOperationException("Call AddTransformStage first.");

        var output = Channel.CreateBounded<EnrichedRecord>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });

        var stage = new EnrichStage(_lastTransformedReader, output.Writer, workers);
        _stages.Add(stage);

        _lastEnrichedReader = output.Reader;
        return this;
    }

    private ChannelReader<EnrichedRecord>? _lastEnrichedReader;

    public PipelineBuilder AddPersistStage(int workers = 2)
    {
        if (_lastEnrichedReader is null)
            throw new InvalidOperationException("Call AddEnrichStage first.");

        var stage = new PersistStage(_lastEnrichedReader, workers);
        _stages.Add(stage);
        return this;
    }

    public Pipeline Build()
    {
        if (_stages.Count == 0)
            throw new InvalidOperationException("No stages added.");

        return new Pipeline(_stages);
    }
}
