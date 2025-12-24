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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using OpenNEL_WinUI.Handlers.Plugin;
using System;
using System.IO;
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
