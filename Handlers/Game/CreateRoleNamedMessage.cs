using System;
using OpenNEL.type;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Serilog;
using OpenNEL.Manager;

namespace OpenNEL_WinUI.Handlers.Game;

public class CreateRoleNamed
{
    public object Execute(string serverId, string name)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(name))
        {
            return new { type = "server_roles_error", message = "参数错误" };
        }
        try
        {
            AppState.X19.CreateCharacter(last.UserId, last.AccessToken, serverId, name);
            if(AppState.Debug)Log.Information("角色创建成功: serverId={ServerId}, name={Name}", serverId, name);
            Entities<EntityGameCharacter> entities = AppState.X19.QueryNetGameCharacters(last.UserId, last.AccessToken, serverId);
            return new { type = "server_roles", entities, serverId, createdName = name };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "角色创建失败: serverId={ServerId}, name={Name}", serverId, name);
            return new { type = "server_roles_error", message = "创建角色失败" };
        }
    }
}
