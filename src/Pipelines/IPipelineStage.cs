namespace Pipelines;

public interface IPipelineStage
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
