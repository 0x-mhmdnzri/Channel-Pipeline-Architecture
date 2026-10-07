namespace Pipelines;

public sealed class Pipeline
{
    private readonly IReadOnlyList<IPipelineStage> _stages;

    public Pipeline(IEnumerable<IPipelineStage> stages)
    {
        _stages = stages.ToList();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _stages.Select(s => s.RunAsync(cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
