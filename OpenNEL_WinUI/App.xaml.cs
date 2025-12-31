/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.IO;
using Microsoft.UI.Xaml;
using Serilog;
using System.Threading.Tasks;
using Codexus.Development.SDK.Manager;
using OpenNEL.Core.Utils;
using Codexus.Interceptors;
using OpenNEL.IRC;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Utils;
using OpenNEL_WinUI.Manager;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.OpenSDK.Entities.Yggdrasil;
using UpdaterService = OpenNEL_WinUI.Updater.Updater;

namespace OpenNEL_WinUI
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
            Task.Run(async () =>
            {
                try
                {
                    ConfigureLogger();
                    AppState.Debug = Debug.Get();
                    AppState.AutoDisconnectOnBan = Manager.SettingManager.Instance.Get().AutoDisconnectOnBan;
                    Log.Information("OpenNEL github: {github}", AppInfo.GithubUrL);
                    Log.Information("版本: {version}", AppInfo.AppVersion);
                    Log.Information("QQ群: {qqgroup}", AppInfo.QQGroup);
                    AppState.Services = await CreateServicesAsync();
                    await AppState.Services.X19.InitializeDeviceAsync();
                    await Utils.Hwid.ReportAsync();
                    await UpdaterService.UpdateAsync(AppInfo.AppVersion);
                    await InitializeSystemComponentsAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "应用初始化失败");
                }
            });
        }
        
        void ConfigureLogger()
        {
            try
            {
                var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
                var logDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logDir);
                var fileName = DateTime.Now.ToString("yyyy-MM-dd-HHmm-ss") + ".log";
                var filePath = Path.Combine(logDir, fileName);
                var isDebug = Manager.SettingManager.Instance.Get().Debug;
                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.Is(isDebug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
                    .WriteTo.Console()
                    .WriteTo.Sink(UiLog.CreateSink())
                    .WriteTo.File(filePath);
                Log.Logger = logConfig.CreateLogger();
                Log.Information("日志已创建: {filePath}, Debug={isDebug}", filePath, isDebug);
            }
            catch (Exception ex)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.Sink(UiLog.CreateSink())
                    .CreateLogger();
                Log.Error(ex, "日志初始化失败");
            }
        }

        static async Task InitializeSystemComponentsAsync()
        {
            var pluginDir = Utils.FileUtil.GetPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            UserManager.Instance.ReadUsersFromDisk();
            Interceptor.EnsureLoaded();
            PacketManager.Instance.RegisterPacketFromAssembly(typeof(App).Assembly);
            PacketManager.Instance.RegisterPacketFromAssembly(typeof(IrcManager).Assembly);
            PacketManager.Instance.EnsureRegistered();
            RegisterIrcHandler();
            _ = Task.Run(() =>
            {
                try
                {
                    PluginManager.Instance.EnsureUninstall();
                    PluginManager.Instance.LoadPlugins(pluginDir);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "插件加载失败");
                }
            });
            await Task.CompletedTask;
        }

        async Task<Services> CreateServicesAsync()
        {
            var c4399 = new C4399();
            var x19 = new X19();
            var crcSalt = await CrcSalt.Compute();
            var yggdrasil = new StandardYggdrasil(new YggdrasilData
            {
                LauncherVersion = x19.GameVersion,
                Channel = "netease",
                CrcSalt = crcSalt
            });
            return new Services(c4399, x19, yggdrasil);
        }

        static void RegisterIrcHandler()
        {
            IrcEventHandler.Register(
                () => AuthManager.Instance.Token ?? "",
                Utils.Hwid.Compute()
            );
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
