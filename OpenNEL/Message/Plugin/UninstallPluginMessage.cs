using OpenNEL.Network;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using OpenNEL.type;
using Serilog;

namespace OpenNEL.Message.Plugin;

internal class UninstallPluginMessage : IWsMessage
{
    public string Type => "uninstall_plugin";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var pluginId = root.TryGetProperty("pluginId", out var idEl) ? idEl.GetString() : null;
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
