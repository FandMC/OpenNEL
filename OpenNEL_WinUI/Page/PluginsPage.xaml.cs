/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using OpenNEL_WinUI.Handlers.Plugin;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using System.Diagnostics;
using OpenNEL_WinUI.Utils;

namespace OpenNEL_WinUI
{
    public sealed partial class PluginsPage : Page
    {
        public static string PageTitle => "插件";

        public ObservableCollection<PluginViewModel> Plugins { get; } = new ObservableCollection<PluginViewModel>();

        public PluginsPage()
        {
            InitializeComponent();
            LoadPlugins();
            _ = CheckUpdatesAsync();
        }

        private void LoadPlugins()
        {
            Plugins.Clear();
            var list = new ListInstalledPlugins().Execute();
            foreach (var item in list)
            {
                Plugins.Add(item);
            }
        }

        private async Task CheckUpdatesAsync()
        {
            try
            {
                var updates = await new CheckPluginUpdates().Execute(Plugins);
                
                foreach (var plugin in Plugins)
                {
                    if (updates.TryGetValue(plugin.Id, out var info))
                    {
                        plugin.NeedUpdate = info.HasUpdate;
                        plugin.LatestVersion = info.LatestVersion;
                        plugin.DownloadUrl = info.DownloadUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "检测更新失败");
            }
        }


        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            new RestartGateway().Execute();
        }

        private async void UpdatePluginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PluginViewModel plugin)
            {
                if (string.IsNullOrEmpty(plugin.DownloadUrl))
                {
                    NotificationHost.ShowGlobal("无法获取下载地址", ToastLevel.Error);
                    return;
                }

                btn.IsEnabled = false;
                btn.Content = "更新中...";
                
                try
                {
                    var infoJson = JsonSerializer.Serialize(new
                    {
                        plugin = new
                        {
                            name = plugin.Name,
                            version = plugin.LatestVersion,
                            downloadUrl = plugin.DownloadUrl
                        }
                    });
                    
                    await new UpdatePlugin().Execute(plugin.Id, plugin.Version, infoJson);
                    
                    plugin.Version = plugin.LatestVersion;
                    plugin.NeedUpdate = false;
                    
                    NotificationHost.ShowGlobal($"插件 {plugin.Name} 更新成功", ToastLevel.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "更新插件失败");
                    NotificationHost.ShowGlobal($"更新失败: {ex.Message}", ToastLevel.Error);
                }
                finally
                {
                    btn.Content = "更新";
                    btn.IsEnabled = true;
                }
            }
        }

        private void UninstallPluginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PluginViewModel plugin)
            {
                try
                {
                    new UninstallPlugin().Execute(plugin.Id);
                    Plugins.Remove(plugin);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Uninstall failed");
                }
            }
        }

        private void OpenStoreButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PluginStorePage));
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
