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
    public object Execute()
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        try
        {
            const int pageSize = 15;
            var offset = 0;
            var servers = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, offset, pageSize);
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
