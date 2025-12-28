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
using OpenNEL.WPFLauncher.Entities.RentalGame;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Entities.Web.RentalGame;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Game.RentalServer;

public class OpenRentalServer
{
    public RentalServerRolesResult Execute(string serverId)
    {
        Log.Debug("[RentalServer] OpenRentalServer.Execute: serverId={ServerId}", serverId);
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null)
        {
            Log.Debug("[RentalServer] OpenRentalServer: 用户未登录");
            return new RentalServerRolesResult { NotLogin = true };
        }
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        }
        try
        {
            if (AppState.Debug) Log.Information("打开租赁服: serverId={ServerId}, account={AccountId}", serverId, last.UserId);
            var entities = AppState.X19.GetRentalGameRolesList(last.UserId, last.AccessToken, serverId);
            var items = entities.Data.Select(r => new RentalRoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new RentalServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取租赁服角色失败: serverId={ServerId}", serverId);
            return new RentalServerRolesResult { Success = false, Message = "获取失败" };
        }
    }

    public RentalServerRolesResult ExecuteForAccount(string accountId, string serverId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        if (string.IsNullOrWhiteSpace(serverId)) return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        try
        {
            var u = UserManager.Instance.GetAvailableUser(accountId);
            if (u == null) return new RentalServerRolesResult { NotLogin = true };
            if (AppState.Debug) Log.Information("打开租赁服: serverId={ServerId}, account={AccountId}", serverId, u.UserId);
            var entities = AppState.X19.GetRentalGameRolesList(u.UserId, u.AccessToken, serverId);
            var items = entities.Data.Select(r => new RentalRoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new RentalServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取租赁服角色失败: serverId={ServerId}", serverId);
            return new RentalServerRolesResult { Success = false, Message = "获取失败" };
        }
    }
}
