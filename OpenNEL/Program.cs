using Codexus.Development.SDK.Manager;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using OpenNEL.Manager;
using OpenNEL.type;
using Serilog;
using OpenNEL.Utils;
using Serilog.Events;
using UpdaterService = OpenNEL.Updater.Updater;

namespace OpenNEL;

internal class Program
{
    static async Task Main(string[] args){
        ConfigureLogger();
        AppState.Debug = Debug.Get();
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
        await Check();
        await UpdaterService.UpdateAsync(AppInfo.AppVersion);

        await new WebSocketServer().StartAsync();
        await InitializeSystemComponentsAsync();
        AppState.Services = await CreateServices();
        await AppState.Services.X19.InitializeDeviceAsync();

        await Task.Delay(Timeout.Infinite);
    }

    static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
    }

    static async Task Check()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        if (PathUtil.ContainsChinese(currentDirectory))
        {
            Log.Error("运行时错误: 当前目录包含中文字符。请将应用程序移动到仅包含英文路径的目录中。");
            Environment.Exit(1);
        }
    }
    
    static async Task InitializeSystemComponentsAsync()
    {
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));
        UserManager.Instance.ReadUsersFromDisk();
        Interceptor.EnsureLoaded();
        PacketManager.Instance.EnsureRegistered();
        PluginManager.Instance.EnsureUninstall();
        PluginManager.Instance.LoadPlugins("plugins");
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
}
