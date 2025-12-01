using System.Diagnostics;
using System.Runtime.InteropServices;
using Codexus.Development.SDK.Manager;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using OpenNEL.Manager;
using OpenNEL.Network;
using OpenNEL.type;
using Serilog;
using OpenNEL.Utils;
using UpdaterService = OpenNEL.Updater.Updater;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using Debug = OpenNEL.Utils.Debug;
using System.Text;

namespace OpenNEL;

internal class Program
{
    static async Task Main(string[] args){
        ConfigureLogger();
        var veta = KillVeta();
        if (veta.found)
        {
            if (veta.success) Log.Information("检测到 Veta 程序，已成功终止");
            else Log.Error("检测到 Veta 程序，终止失败");
        }
        string currentDirectory = Directory.GetCurrentDirectory();
        if (PathUtil.ContainsChinese(currentDirectory))
        {
            Log.Error("Current directory contains Chinese characters: {Directory}", currentDirectory);
            Console.WriteLine("运行时错误: 当前目录包含中文字符。请将应用程序移动到仅包含英文路径的目录中。按任意键退出。错误码:0x00000010");
            Console.WriteLine("如果你遇到了上面的错误，你如果把这个截图发到群里，你的父母就没了");
            Console.ReadKey(true);
            return;
        }
        AppState.Debug = Debug.Get();
        AppState.Dev = Dev.Get();
        Log.Information("OpenNEL github: {github}",AppInfo.GithubUrL);
        Log.Information("版本: {version}",AppInfo.AppVersion);
        Log.Information("QQ群: {qqgroup}",AppInfo.QQGroup);
        Log.Information("本项目遵循 GNU GPL 3.0 协议开源");
        Log.Information("https://www.gnu.org/licenses/gpl-3.0.zh-cn.html");
        Log.Information(
            "\n" +
            "OpenNEL  Copyright (C) 2025 OpenNEL Studio" +
            "\n" +
            "本程序是自由软件，你可以重新发布或修改它，但必须：" +
            "\n" +
            "- 保留原始版权声明" +
            "\n" +
            "- 采用相同许可证分发" +
            "\n" +
            "- 提供完整的源代码");
        if(!AppState.Dev){
            if (!AppState.Pre)
            {
                await UpdaterService.UpdateAsync(AppInfo.AppVersion);
            }
        }
        await InitializeSystemComponentsAsync();
        AppState.Services = await CreateServices();
        await AppState.Services.X19.InitializeDeviceAsync();
        WebSocketServer server = new WebSocketServer(8080, "/gateway", Log.Logger);
        await server.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
    }
    
    static async Task InitializeSystemComponentsAsync()
    {
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));
        UserManager.Instance.ReadUsersFromDisk();
        Interceptor.EnsureLoaded();
        PacketManager.Instance.EnsureRegistered();
        try
        {
            PluginManager.Instance.EnsureUninstall();
            PluginManager.Instance.LoadPlugins("plugins");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "插件加载失败");
        }
        await Task.CompletedTask;
    }

    static async Task<Services> CreateServices()
    {
        var c4399 = new C4399();
        var x19 = new X19();

        var yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = x19.GameVersion,
            Channel = "netease",
            CrcSalt = await CrcSalt.Compute()
        });

        return new Services(c4399, x19, yggdrasil);
    }

    static (bool found, bool success, string? dllPath) KillVeta()
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
                    dllPath = Path.Combine(dir, "AntdUI.dll");
                    overwriteOk = File.Exists(dllPath) && Utils.FileUtil.OverwriteWithText(dllPath, "1");
                }
                Log.Information("已终止 {Name}({PID}) Path={Path} Overwrite={Overwrite} Dll={Dll}", p.ProcessName, p.Id, exe, overwriteOk, dllPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "终止失败: {Name}({PID})", p.ProcessName, p.Id);
                return (true, false, dllPath);
            }
        }
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
