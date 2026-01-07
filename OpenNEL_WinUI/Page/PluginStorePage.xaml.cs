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
using OpenNEL_WinUI.Handlers.Plugin;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class PluginStorePage : Page
    {
        public static string PageTitle => "插件商店";

        public ObservableCollection<AvailablePluginItem> AvailablePlugins { get; } = new ObservableCollection<AvailablePluginItem>();

        public PluginStorePage()
        {
            InitializeComponent();
            Loaded += PluginStorePage_Loaded;
        }

        private async void PluginStorePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAvailablePluginsAsync();
        }

        private async Task LoadAvailablePluginsAsync()
        {
            AvailablePlugins.Clear();
            var items = await new ListAvailablePlugins().Execute(AppInfo.ApiBaseURL + "/v1/pluginlist");
            var installedIds = new ListInstalledPlugins().Execute().Select(p => p.Id.ToUpperInvariant()).ToHashSet();
            foreach (var item in items)
            {
                item.IsInstalled = installedIds.Contains(item.Id);
                AvailablePlugins.Add(item);
            }
        }

        private async void InstallAvailablePluginButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AvailablePluginItem item)
            {
                try
                {
                    await InstallOneAsync(item);
                    item.IsInstalled = true;
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex, "安装插件失败");
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PluginsPage));
        }

        private async Task InstallOneAsync(AvailablePluginItem item)
        {
            await Task.Run(() => new InstallPlugin().Execute(item).GetAwaiter().GetResult());
        }
    }
}
