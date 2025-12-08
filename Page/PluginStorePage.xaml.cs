using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenNEL_WinUI.Handlers.Plugin;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Development.SDK.Manager;
using Serilog;

namespace OpenNEL_WinUI
{
    public sealed partial class PluginStorePage : Page
    {
        public static string PageTitle => "插件商店";

        public ObservableCollection<AvailablePluginItem> AvailablePlugins { get; } = new ObservableCollection<AvailablePluginItem>();

        public PluginStorePage()
        {
            this.InitializeComponent();
            this.Loaded += PluginStorePage_Loaded;
        }

        private async void PluginStorePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAvailablePluginsAsync();
        }

        private async Task LoadAvailablePluginsAsync()
        {
            AvailablePlugins.Clear();
            var obj = await new ListAvailablePlugins().Execute("https://api.fandmc.cn/v1/pluginlist");
            var itemsProp = obj.GetType().GetProperty("items");
            var arr = itemsProp != null ? itemsProp.GetValue(obj) as System.Array : null;
            var installedIds = PluginHandler.GetInstalledPlugins().Select(p => p.Id.ToUpperInvariant()).ToHashSet();
            if (arr != null)
            {
                foreach (var it in arr)
                {
                    var id = GetPropString(it, "id")?.ToUpperInvariant() ?? string.Empty;
                    var item = new AvailablePluginItem
                    {
                        Id = id,
                        Name = GetPropString(it, "name") ?? string.Empty,
                        Version = GetPropString(it, "version") ?? string.Empty,
                        LogoUrl = GetPropString(it, "logoUrl") ?? string.Empty,
                        ShortDescription = GetPropString(it, "shortDescription") ?? string.Empty,
                        Publisher = GetPropString(it, "publisher") ?? string.Empty,
                        DownloadUrl = GetPropString(it, "downloadUrl") ?? string.Empty,
                        Depends = (GetPropString(it, "depends") ?? string.Empty).ToUpperInvariant(),
                        IsInstalled = installedIds.Contains(id)
                    };
                    AvailablePlugins.Add(item);
                }
            }
        }

        private static string GetPropString(object o, string name)
        {
            var p = o.GetType().GetProperty(name);
            var v = p != null ? p.GetValue(o) : null;
            return v != null ? v.ToString() : null;
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
            var payload = JsonSerializer.Serialize(new
            {
                plugin = new
                {
                    id = item.Id,
                    name = item.Name,
                    version = item.Version,
                    downloadUrl = item.DownloadUrl,
                    depends = item.Depends
                }
            });
            await Task.Run(() => PluginHandler.InstallPluginByInfo(payload));
        }
    }
}
