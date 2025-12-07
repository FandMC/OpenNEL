using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using OpenNEL_WinUI.Handlers.Plugin;
using System;
using System.IO;
using Serilog;
using System.Diagnostics;
using OpenNEL.Utils;

namespace OpenNEL_WinUI
{
    public sealed partial class PluginsPage : Page
    {
        public static string PageTitle => "插件";

        public ObservableCollection<PluginViewModel> Plugins { get; } = new ObservableCollection<PluginViewModel>();

        public PluginsPage()
        {
            this.InitializeComponent();
            LoadPlugins();
        }

        private void LoadPlugins()
        {
            Plugins.Clear();
            var list = PluginHandler.GetInstalledPlugins();
            foreach (var item in list)
            {
                Plugins.Add(item);
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            PluginHandler.RestartGateway();
        }

        private void UpdatePluginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PluginViewModel plugin)
            {
            }
        }

        private void UninstallPluginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PluginViewModel plugin)
            {
                try
                {
                    PluginHandler.UninstallPlugin(plugin.Id);
                    
                    plugin.IsWaitingRestart = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Uninstall failed");
                }
            }
        }

        private void OpenPluginsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dir = FileUtil.GetPluginDirectory();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "打开插件目录失败");
            }
        }
    }
}
