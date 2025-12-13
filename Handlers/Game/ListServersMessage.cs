using System.Linq;
using OpenNEL.Utils;
using Serilog;
using OpenNEL.type;
using OpenNEL.Manager;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;

namespace OpenNEL_WinUI.Handlers.Game;

public class ListServers
{
    public object Execute(int offset, int pageSize)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        try
        {
            var servers = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, offset, pageSize);
            if(AppState.Debug) Log.Information("服务器列表: 数量={Count}", servers.Data?.Length ?? 0);
            var data = servers.Data ?? System.Array.Empty<EntityNetGameItem>();
            var items = data.Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            var hasMore = data.Length >= pageSize;
            return new { type = "servers", items, hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new { type = "servers_error", message = "获取失败" };
        }
    }
}
