using System;
using System.IO;
using Microsoft.UI.Xaml;
using Serilog;
using System.Threading.Tasks;
using Codexus.Development.SDK.Manager;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using OpenNEL.type;
using OpenNEL.Utils;
using OpenNEL.Manager;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.OpenSDK.Entities.Yggdrasil;
using UpdaterService = OpenNEL.Updater.Updater;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenNEL_WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
            Task.Run(async () =>
            {
                try
                {
                    ConfigureLogger();
                    await Hwid.ReportAsync();
                    KillVeta.Run();
                    var currentDirectory = Directory.GetCurrentDirectory();
                    if (PathUtil.ContainsChinese(currentDirectory))
                    {
                        Log.Error("Current directory contains Chinese characters: {Directory}", currentDirectory);
                        Environment.Exit(0);
                        return;
                    }
                    AppState.Debug = Debug.Get();
                    AppState.Dev = Dev.Get();
                    Log.Information("OpenNEL github: {github}", AppInfo.GithubUrL);
                    Log.Information("版本: {version}", AppInfo.AppVersion);
                    Log.Information("QQ群: {qqgroup}", AppInfo.QQGroup);
                    if (!AppState.Dev)
                    {
                        if (!AppState.Pre)
                        {
                            await UpdaterService.UpdateAsync(AppInfo.AppVersion);
                        }
                    }
                    await InitializeSystemComponentsAsync();
                    AppState.Services = await CreateServicesAsync();
                    await AppState.Services.X19.InitializeDeviceAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "应用初始化失败");
                }
            });
        }

        void ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();
        }

        static async Task InitializeSystemComponentsAsync()
        {
            var pluginDir = OpenNEL.Utils.FileUtil.GetPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            UserManager.Instance.ReadUsersFromDisk();
            Interceptor.EnsureLoaded();
            PacketManager.Instance.EnsureRegistered();
            try
            {
                PluginManager.Instance.EnsureUninstall();
                PluginManager.Instance.LoadPlugins(pluginDir);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "插件加载失败");
            }
            await Task.CompletedTask;
        }

        async Task<Services> CreateServicesAsync()
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
        
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Error(e.Exception, "未处理异常");
            }
            catch { }
            e.Handled = true;
        }
    }
}
