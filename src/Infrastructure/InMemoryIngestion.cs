using System.Threading.Channels;
using Models;

namespace Infrastructure;

/// <summary>
/// Demo ingestion that generates synthetic records.
/// Replace with Kafka / File / HTTP / SQL reader in real scenarios.
/// </summary>
public sealed class InMemoryIngestion
{
    private readonly ChannelWriter<RawRecord> _writer;
    private readonly long _totalRecords;

    public InMemoryIngestion(ChannelWriter<RawRecord> writer, long totalRecords = 100_000)
    {
        _writer = writer;
        _totalRecords = totalRecords;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        for (long i = 1; i <= _totalRecords; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = new RawRecord(i, $"payload-{i}", DateTimeOffset.UtcNow);
            await _writer.WriteAsync(record, cancellationToken).ConfigureAwait(false);

            // Optional: slight throttle for demo
            // if (i % 10_000 == 0) await Task.Delay(1, cancellationToken);
        }

        _writer.Complete();
        Console.WriteLine($"[Ingestion] Finished producing {_totalRecords:N0} records");
    }
}
