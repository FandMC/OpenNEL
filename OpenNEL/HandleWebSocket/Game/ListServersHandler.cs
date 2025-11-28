using OpenNEL.network;
using OpenNEL.Utils;
using System.Text.Json;
using Serilog;
using OpenNEL.type;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

namespace OpenNEL.HandleWebSocket.Game;

internal class ListServersHandler : IWsHandler
{
    public string Type => "list_servers";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            return new { type = "notlogin" };
        }
        try
        {
            const int pageSize = 15;
            var offset = 0;
            var servers = await auth.Api<EntityNetGameRequest, Entities<EntityNetGameItem>>(
                "/item/query/available",
                new EntityNetGameRequest
                {
                    AvailableMcVersions = Array.Empty<string>(),
                    ItemType = 1,
                    Length = pageSize,
                    Offset = offset,
                    MasterTypeId = "2",
                    SecondaryTypeId = ""
                });
            
            if(AppState.Debug) Log.Information("服务器列表: 数量={Count}", servers.Data?.Length ?? 0);
            var items = servers.Data.Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            return new { type = "servers", items };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new { type = "servers_error", message = "获取失败" };
        }
    }
}
