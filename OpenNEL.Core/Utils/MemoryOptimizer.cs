using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Serilog;

namespace OpenNEL.Core.Utils;

public class MemoryOptimizer : IDisposable
{
    private const uint ProcessQueryInformation = 1024u;
    private const uint ProcessSetQuota = 256u;

    private static MemoryOptimizer? _instance;
    private static readonly Lock Lock = new();

    private Timer? _optimizationTimer;
    private readonly HashSet<int> _processedIds = new();
    private readonly Lock _lockObject = new();
    private bool _disposed;

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(nint hProcess, nint dwMinimumWorkingSetSize, nint dwMaximumWorkingSetSize);

    [DllImport("psapi.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyWorkingSet(nint hProcess);

    private MemoryOptimizer()
    {
        _optimizationTimer = new Timer(OptimizeCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes(7));
    }

    public static MemoryOptimizer GetInstance()
    {
        using (Lock.EnterScope())
        {
            return _instance ??= new MemoryOptimizer();
        }
    }

    private void OptimizeCallback(object? state)
    {
        if (_disposed || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Dispose();
            return;
        }
        try
        {
            List<Process> minecraftProcesses = GetMinecraftProcesses();
            if (minecraftProcesses.Count == 0)
            {
                Log.Information("[Memory Optimizer] No Minecraft processes found, stopping optimizer");
                Dispose();
                return;
            }
            foreach (Process item in minecraftProcesses)
            {
                OptimizeProcess(item);
            }
            CleanupExitedProcesses();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Memory Optimizer] Error in optimization callback");
        }
    }

    private static List<Process> GetMinecraftProcesses()
    {
        List<Process> list = new();
        try
        {
            Process[] processesByName = Process.GetProcessesByName("javaw");
            list.AddRange(processesByName.Where(IsMinecraftProcess));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Memory Optimizer] Failed to get Minecraft processes");
        }
        return list;
    }

    private static bool IsMinecraftProcess(Process process)
    {
        try
        {
            string? commandLine = GetCommandLine(process);
            if (string.IsNullOrEmpty(commandLine))
            {
                return false;
            }
            string[] keywords = ["minecraft", "net.minecraft", "launchwrapper", "forge", "fabric", "quilt", "optifine", ".minecraft", "versions", "libraries"];
            return keywords.Any(keyword => commandLine.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Memory Optimizer] Failed to check if process is Minecraft: {ProcessId}", process.Id);
            return false;
        }
    }

    private static string? GetCommandLine(Process process)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }
            using ManagementObjectSearcher searcher = new($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
            using ManagementObjectCollection results = searcher.Get();
            foreach (ManagementBaseObject obj in results)
            {
                if (obj is ManagementObject mo)
                {
                    return mo["CommandLine"]?.ToString() ?? "";
                }
            }
        }
        catch
        {
            try
            {
                return process.StartInfo.Arguments;
            }
            catch
            {
                Log.Error("[Memory Optimizer] Failed to get CommandLine arguments");
                return null;
            }
        }
        return "";
    }

    private void OptimizeProcess(Process process)
    {
        if (_disposed)
        {
            return;
        }
        using (_lockObject.EnterScope())
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }
                process.Refresh();
                long workingSet = process.WorkingSet64;
                long memoryMb = workingSet / 1048576;
                nint handle = OpenProcess(1280u, bInheritHandle: false, (uint)process.Id);
                if (handle == IntPtr.Zero)
                {
                    return;
                }
                try
                {
                    if (EmptyWorkingSet(handle))
                    {
                        long minSize = Math.Max(52428800L, workingSet / 4);
                        long maxSize = Math.Max(minSize * 2, workingSet);
                        SetProcessWorkingSetSize(handle, new IntPtr(minSize), new IntPtr(maxSize));
                        Thread.Sleep(500);
                        process.Refresh();
                        long afterMb = process.WorkingSet64 / 1048576;
                        Log.Information("[Memory Optimizer] Process ID: {ProcessId} - Memory Before: {BeforeMemory} MB, After: {AfterMemory} MB", process.Id, memoryMb, afterMb);
                        _processedIds.Add(process.Id);
                    }
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Memory Optimizer] Failed to optimize process {ProcessId}", process.Id);
            }
        }
    }

    private void CleanupExitedProcesses()
    {
        if (_disposed)
        {
            return;
        }
        using (_lockObject.EnterScope())
        {
            List<int> toRemove = new();
            foreach (int processedId in _processedIds)
            {
                try
                {
                    Process process = Process.GetProcessById(processedId);
                    if (process.HasExited)
                    {
                        toRemove.Add(processedId);
                        process.Dispose();
                    }
                }
                catch (ArgumentException)
                {
                    toRemove.Add(processedId);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[Memory Optimizer] Error checking process {ProcessId}", processedId);
                    toRemove.Add(processedId);
                }
            }
            foreach (int item in toRemove)
            {
                _processedIds.Remove(item);
            }
            if (toRemove.Count > 0)
            {
                Log.Information("[Memory Optimizer] Cleaned up {Count} exited processes", toRemove.Count);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        using (Lock.EnterScope())
        {
            if (!_disposed)
            {
                _disposed = true;
                _optimizationTimer?.Dispose();
                _optimizationTimer = null;
                _instance = null;
                Log.Information("[Memory Optimizer] Disposed");
            }
        }
    }
}
