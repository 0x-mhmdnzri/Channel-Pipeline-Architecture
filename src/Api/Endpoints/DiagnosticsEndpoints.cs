using Api.Concurrency;

namespace Api.Endpoints;

public static class DiagnosticsEndpoints
{
    public static RouteGroupBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diagnostics").WithTags("Diagnostics");

        group.MapGet("/cpu", (MutationQueue queue) =>
        {
            return Results.Ok(new CpuDiag(
                Environment.ProcessorCount,
                queue.PinnedCore,
                queue.CurrentFence,
                SimdUtil.IsAvx2,
                SimdUtil.IsSse2,
                SimdUtil.VectorSizeBytes,
                System.Numerics.Vector.IsHardwareAccelerated));
        })
        .WithName("CpuDiagnostics")
        .WithSummary("Thread affinity + SIMD capability report");

        group.MapGet("/simd-bench", () =>
        {
            var buf = new byte[64 * 1024];
            Random.Shared.NextBytes(buf);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            const int iters = 2000;
            ulong last = 0;
            for (int i = 0; i < iters; i++)
                last = SimdUtil.Checksum(buf);
            sw.Stop();
            return Results.Ok(new SimdBench(
                Iters: iters,
                BytesPerIter: buf.Length,
                TotalMs: sw.Elapsed.TotalMilliseconds,
                ThroughputGBps: (iters * (double)buf.Length / 1e9) / sw.Elapsed.TotalSeconds,
                Checksum: last,
                Avx2: SimdUtil.IsAvx2));
        })
        .WithName("SimdBench");

        return group;
    }
}

public sealed record CpuDiag(
    int ProcessorCount,
    int MutationPinnedCore,
    long CurrentFence,
    bool Avx2,
    bool Sse2,
    int VectorByteWidth,
    bool VectorHardwareAccelerated);

public sealed record SimdBench(
    int Iters,
    int BytesPerIter,
    double TotalMs,
    double ThroughputGBps,
    ulong Checksum,
    bool Avx2);
