using OpenNEL.network;
using OpenNEL.type;
using OpenNEL.Utils;
using System.Text.Json;
using Serilog;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

namespace OpenNEL.HandleWebSocket.Game;

internal class SearchServersHandler : IWsHandler
{
    public string Type => "search_servers";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var keyword = root.TryGetProperty("keyword", out var k) ? k.GetString() : string.Empty;
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth))
        {
            return new { type = "notlogin" };
        }
        try
        {
            var servers = await auth.Api<EntityNetGameKeyword, Entities<EntityNetGameItem>>(
                "/item/query/search-by-keyword",
                new EntityNetGameKeyword { Keyword = keyword ?? string.Empty });
            
            if(AppState.Debug)Log.Information("服务器搜索: 关键字={Keyword}, 数量={Count}", keyword, servers.Data?.Length ?? 0);
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
