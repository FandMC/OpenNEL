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
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Utils; 
using OpenNEL_WinUI.Manager;
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

    public object ExecuteForAccount(string accountId, string serverId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return new { type = "server_roles_error", message = "参数错误" };
        if (string.IsNullOrWhiteSpace(serverId)) return new { type = "server_roles_error", message = "参数错误" };
        try
        {
            var u = UserManager.Instance.GetAvailableUser(accountId);
            if (u == null) return new { type = "notlogin" };
            if(AppState.Debug)Log.Information("打开服务器: serverId={ServerId}, account={AccountId}", serverId, u.UserId);
            Entities<EntityGameCharacter> entities = AppState.X19.QueryNetGameCharacters(u.UserId, u.AccessToken, serverId);
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
