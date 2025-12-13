using System;
using System.Linq;
using OpenNEL.type;
using OpenNEL.Manager;
using OpenNEL.Utils;
using Serilog;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

namespace OpenNEL_WinUI.Handlers.Game;

public class SearchServers
{
    public object Execute(string keyword, int offset, int pageSize)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        try
        {
            var all = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, 0, 500);
            var data = all.Data ?? Array.Empty<EntityNetGameItem>();
            var q = string.IsNullOrWhiteSpace(keyword) ? data : data.Where(s => (s.Name ?? string.Empty).IndexOf(keyword!, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if(AppState.Debug)Log.Information("服务器搜索: 关键字={Keyword}, 数量={Count}", keyword, q.Length);
            var pageItems = q.Skip(offset).Take(pageSize).Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            var hasMore = offset + pageSize < q.Length;
            return new { type = "servers", items = pageItems, hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new { type = "servers_error", message = "获取失败" };
        }
    }
}
