/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Linq;
using OpenNEL_WinUI.type;
using OpenNEL.WPFLauncher.Entities;
using OpenNEL.WPFLauncher.Entities.NetGame;
using Serilog;
using OpenNEL_WinUI.Manager;

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
            var items = entities.Data.Select(r => new { id = r.Name, name = r.Name }).ToArray();
            return new { type = "server_roles", items, serverId, createdName = name };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "角色创建失败: serverId={ServerId}, name={Name}", serverId, name);
            return new { type = "server_roles_error", message = "创建角色失败" };
        }
    }
}
