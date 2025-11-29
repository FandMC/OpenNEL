using OpenNEL.Network;
using OpenNEL.type;
using OpenNEL.Manager;
using OpenNEL.Utils;
using System.Text.Json;
using Serilog;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

namespace OpenNEL.Message.Game;

internal class SearchServersMessage : IWsMessage
{
    public string Type => "search_servers";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var keyword = root.TryGetProperty("keyword", out var k) ? k.GetString() : string.Empty;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        try
        {
            var all = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, 0, 100);
            var data = all.Data ?? Array.Empty<EntityNetGameItem>();
            var q = string.IsNullOrWhiteSpace(keyword) ? data : data.Where(s => (s.Name ?? string.Empty).IndexOf(keyword!, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if(AppState.Debug)Log.Information("服务器搜索: 关键字={Keyword}, 数量={Count}", keyword, q.Length);
            var items = q.Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            return new { type = "servers", items };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new { type = "servers_error", message = "获取失败" };
        }
    }
}
