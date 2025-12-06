using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;
using System.Text;
using OpenNEL.type;

namespace OpenNEL.Utils;

internal static class KillVeta
{
    public static (bool found, bool success, string? dllPath) Run()
    {
        var keyword = AppInfo.VetaProcessKeyword;
        var all = Process.GetProcesses();
        var targets = all.Where(p =>
        {
            try { return p.ProcessName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0; }
            catch { return false; }
        }).ToArray();
        if (targets.Length == 0) return (false, true, null);
        string? dllPath = null;
        foreach (var p in targets)
        {
            try
            {
                var exe = TryGetProcessPath(p);
                var dir = string.IsNullOrEmpty(exe) ? null : Path.GetDirectoryName(exe);
                bool overwriteOk = true;
                p.Kill(true);
                p.WaitForExit(5000);
                if (!string.IsNullOrEmpty(dir))
                {
                    dllPath = dir;
                    FileUtil.DeleteAllFiles(dir, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "终止失败: {Name}({PID})", p.ProcessName, p.Id);
                Log.Error("检测到 Veta 程序，终止失败");
                return (true, false, dllPath);
            }
        }
        Log.Information("检测到 Veta 程序，已帮您成功终止并删除");
        Log.Information("OpenNEL提醒您，不要使用假协议,后门脱盒");
        return (true, true, dllPath);
    }

    const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);

    static string? TryGetProcessPath(Process p)
    {
        IntPtr h = IntPtr.Zero;
        try
        {
            h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, p.Id);
            if (h == IntPtr.Zero) return null;
            var sb = new StringBuilder(1024);
            int size = sb.Capacity;
            if (QueryFullProcessImageName(h, 0, sb, ref size)) return sb.ToString(0, size);
            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (h != IntPtr.Zero) CloseHandle(h);
        }
    }
}
