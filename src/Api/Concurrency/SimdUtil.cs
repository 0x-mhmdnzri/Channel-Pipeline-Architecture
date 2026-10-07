using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Api.Concurrency;

/// <summary>
/// SIMD helpers (AVX2 / SSE2 / Vector&lt;T&gt;) for hot-path bulk work.
/// </summary>
public static class SimdUtil
{
    public static bool IsAvx2 => Avx2.IsSupported;
    public static bool IsSse2 => Sse2.IsSupported;
    public static int VectorSizeBytes => Vector<byte>.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Checksum(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return 0;

        if (Avx2.IsSupported && data.Length >= 32)
            return ChecksumAvx2(data);

        if (Vector.IsHardwareAccelerated && data.Length >= Vector<byte>.Count)
            return ChecksumVector(data);

        ulong h = 0;
        foreach (var b in data)
            h = (h * 31) + b;
        return h;
    }

    private static unsafe ulong ChecksumAvx2(ReadOnlySpan<byte> data)
    {
        fixed (byte* p = data)
        {
            var acc = Vector256<byte>.Zero;
            int i = 0;
            int lim = data.Length - 31;
            for (; i < lim; i += 32)
            {
                var v = Avx.LoadVector256(p + i);
                acc = Avx2.Xor(acc, v);
            }

            ulong h = 0;
            var tmp = stackalloc byte[32];
            Avx.Store(tmp, acc);
            for (int j = 0; j < 32; j++)
                h ^= (ulong)tmp[j] << ((j & 7) * 8);

            for (; i < data.Length; i++)
                h = (h * 31) + p[i];

            return h;
        }
    }

    private static ulong ChecksumVector(ReadOnlySpan<byte> data)
    {
        var acc = Vector<byte>.Zero;
        int w = Vector<byte>.Count;
        int i = 0;
        for (; i <= data.Length - w; i += w)
        {
            var v = new Vector<byte>(data.Slice(i, w));
            acc ^= v;
        }

        Span<byte> fold = stackalloc byte[w];
        acc.CopyTo(fold);
        ulong h = 0;
        for (int j = 0; j < w; j++)
            h ^= (ulong)fold[j] << ((j & 7) * 8);

        for (; i < data.Length; i++)
            h = (h * 31) + data[i];

        return h;
    }

    public static int CountGreaterOrEqual(ReadOnlySpan<int> values, int threshold)
    {
        if (values.IsEmpty) return 0;

        if (Vector.IsHardwareAccelerated && values.Length >= Vector<int>.Count)
            return CountGeVector(values, threshold);

        int c = 0;
        foreach (var v in values)
            if (v >= threshold) c++;
        return c;
    }

    private static int CountGeVector(ReadOnlySpan<int> values, int threshold)
    {
        int w = Vector<int>.Count;
        var thr = new Vector<int>(threshold);
        int count = 0, i = 0;
        for (; i <= values.Length - w; i += w)
        {
            var v = new Vector<int>(values.Slice(i, w));
            var mask = Vector.GreaterThanOrEqual(v, thr);
            for (int j = 0; j < w; j++)
                if (mask[j] != 0) count++;
        }
        for (; i < values.Length; i++)
            if (values[i] >= threshold) count++;
        return count;
    }

    public static void Fill(Span<byte> dest, byte value)
    {
        if (dest.IsEmpty) return;
        if (Vector.IsHardwareAccelerated && dest.Length >= Vector<byte>.Count)
        {
            var v = new Vector<byte>(value);
            int w = Vector<byte>.Count;
            int i = 0;
            for (; i <= dest.Length - w; i += w)
                v.CopyTo(dest.Slice(i, w));
            for (; i < dest.Length; i++)
                dest[i] = value;
            return;
        }
        dest.Fill(value);
    }
}
