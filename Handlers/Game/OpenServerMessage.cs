using System;
using System.Linq;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities;
using OpenNEL.type;
using OpenNEL.Utils; 
using OpenNEL.Manager;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Game;

public class OpenServer
{
    public object Execute(string serverId)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return new { type = "server_roles_error", message = "参数错误" };
        }
        try
        {
            if(AppState.Debug)Log.Information("打开服务器: serverId={ServerId}, account={AccountId}", serverId, last.UserId);
            Entities<EntityGameCharacter> entities = AppState.X19.QueryNetGameCharacters(last.UserId, last.AccessToken, serverId);
            var items = entities.Data.Select(r => new { id = r.Name, name = r.Name }).ToArray();
            return new { type = "server_roles", items, serverId };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取服务器角色失败: serverId={ServerId}", serverId);
            return new { type = "server_roles_error", message = "获取失败" };
        }
    }
}
