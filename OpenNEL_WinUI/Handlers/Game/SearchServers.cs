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
using System.Collections.Generic;
using System.Linq;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Utils;
using Serilog;
using OpenNEL.WPFLauncher.Entities.NetGame;
using OpenNEL.WPFLauncher.Entities;

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
            var data = all.Data ?? new List<EntityNetGameItem>();
            var q = string.IsNullOrWhiteSpace(keyword) ? data.ToList() : data.Where(s => (s.Name ?? string.Empty).IndexOf(keyword!, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if(AppState.Debug)Log.Information("服务器搜索: 关键字={Keyword}, 数量={Count}", keyword, q.Count);
            var pageItems = q.Skip(offset).Take(pageSize).Select(s => new { entityId = s.EntityId, name = s.Name }).ToArray();
            var hasMore = offset + pageSize < q.Count;
            return new { type = "servers", items = pageItems, hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new { type = "servers_error", message = "获取失败" };
        }
    }
}
