using System.Linq;
using Codexus.Development.SDK.Manager;
using OpenNEL.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class UninstallPlugin
    {
        public object Execute(string pluginId)
        {
            if (!string.IsNullOrWhiteSpace(pluginId))
            {
                Log.Information("卸载插件 {PluginId}", pluginId);
                PluginManager.Instance.UninstallPlugin(pluginId);
                AppState.WaitRestartPlugins[pluginId] = true;
            }
            var updPayload = new { type = "installed_plugins_updated" };
            var items = PluginManager.Instance.Plugins.Values.Select(plugin => new {
                identifier = plugin.Id,
                name = plugin.Name,
                version = plugin.Version,
                description = plugin.Description,
                author = plugin.Author,
                status = plugin.Status
            }).ToArray();
            var listPayload = new { type = "installed_plugins", items };
            return new object[] { updPayload, listPayload };
        }
    }
}
