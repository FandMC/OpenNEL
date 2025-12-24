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
using System.Linq;
using OpenNEL_WinUI.Utils;
using Serilog;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Manager;
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities;

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
            if(AppState.Debug) Log.Information("服务器列表: 数量={Count}", servers.Data?.Count ?? 0);
            var data = servers.Data ?? new System.Collections.Generic.List<EntityNetGameItem>();
            var items = data.Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            var hasMore = data.Count >= pageSize;
            return new { type = "servers", items, hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new { type = "servers_error", message = "获取失败" };
        }
    }
}
