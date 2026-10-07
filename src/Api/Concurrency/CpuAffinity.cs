using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Api.Concurrency;

/// <summary>
/// Pin threads to specific CPU cores (Windows + Linux).
/// Reduces cache-line migration and context-switch jitter.
/// </summary>
public static class CpuAffinity
{
    public static bool TryPinCurrentThread(int coreIndex)
    {
        if (coreIndex < 0 || coreIndex >= Environment.ProcessorCount)
            return false;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return PinWindows(coreIndex);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return PinLinux(coreIndex);

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static int PreferredMutationCore =>
        Math.Max(0, Environment.ProcessorCount - 1);

    [SupportedOSPlatform("windows")]
    private static bool PinWindows(int coreIndex)
    {
        var mask = new IntPtr(1L << coreIndex);
        var handle = GetCurrentThread();
        var result = SetThreadAffinityMask(handle, mask);
        return result != IntPtr.Zero;
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll")]
    private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

    private static bool PinLinux(int coreIndex)
    {
        Span<byte> set = stackalloc byte[128];
        set.Clear();
        set[coreIndex / 8] = (byte)(1 << (coreIndex % 8));
        var rc = SchedSetAffinity(0, (IntPtr)set.Length, ref MemoryMarshal.GetReference(set));
        return rc == 0;
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "sched_setaffinity")]
    private static extern int SchedSetAffinity(int pid, IntPtr cpusetsize, ref byte mask);

    public static void LogPinResult(bool ok, int core, string role)
    {
        var msg = ok
            ? $"[CpuAffinity] {role} pinned to core {core}"
            : $"[CpuAffinity] {role} pin to core {core} FAILED (continuing unpinned)";
        Console.WriteLine(msg);
        Debug.WriteLine(msg);
    }
}
