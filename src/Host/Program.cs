using System.Diagnostics;
using System.Threading.Channels;
using Infrastructure;
using Models;
using Pipelines;

// ============================================================
// Channel Pipeline Architecture - Demo Host
// ============================================================

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Channel Pipeline Architecture - High Volume Demo");
Console.WriteLine("Press Ctrl+C to stop\n");

// 1. Create the entry channel (bounded for backpressure)
var ingestionChannel = Channel.CreateBounded<RawRecord>(new BoundedChannelOptions(5_000)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
    SingleReader = false
});

// 2. Build the pipeline
var pipeline = new PipelineBuilder()
    .WithIngestion(ingestionChannel.Reader)
    .AddTransformStage(workers: 8, capacity: 2_000)
    .AddEnrichStage(workers: 4, capacity: 2_000)
    .AddPersistStage(workers: 2)
    .Build();

// 3. Start ingestion (producer)
var ingestion = new InMemoryIngestion(ingestionChannel.Writer, totalRecords: 100_000);
var ingestionTask = ingestion.RunAsync(cts.Token);

// 4. Start the pipeline (consumers)
var sw = Stopwatch.StartNew();
var pipelineTask = pipeline.RunAsync(cts.Token);

try
{
    await Task.WhenAll(ingestionTask, pipelineTask).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nCancelled by user.");
}

sw.Stop();
Console.WriteLine($"\nDone in {sw.Elapsed.TotalSeconds:F2} seconds");
