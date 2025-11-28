using OpenNEL.network;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.type;

namespace OpenNEL.HandleWebSocket.Plugin;

internal class ListInstalledPluginsHandler : IWsHandler
{
    public string Type => "list_installed_plugins";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        List<EntityPluginsResponse> items = PluginManager.Instance.Plugins.Values.Select(plugin => new EntityPluginsResponse{
            PluginId = plugin.Id,
            PluginName = plugin.Name,
            PluginDescription = plugin.Description,
            PluginVersion = plugin.Version,
            PluginAuthor = plugin.Author,
            PluginStatus = plugin.Status,
            PluginWaitingRestart = AppState.WaitRestartPlugins.ContainsKey(plugin.Id)
        }).ToList();
        return new { type = "installed_plugins", items };
    }
}
