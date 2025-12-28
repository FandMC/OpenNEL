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
using OpenNEL_WinUI.Entities.Web.NetGame;

namespace OpenNEL_WinUI.Handlers.Game.NetServer;

public class SearchServers
{
    public ListServersResult Execute(string keyword, int offset, int pageSize)
    {
        Log.Debug("[SearchServers] Execute: keyword={Keyword}, offset={Offset}, pageSize={PageSize}", keyword, offset, pageSize);
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null)
        {
            Log.Debug("[SearchServers] 用户未登录");
            return new ListServersResult { NotLogin = true };
        }
        try
        {
            var all = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, 0, 500);
            Log.Debug("[SearchServers] API 返回: Code={Code}, DataCount={Count}", all.Code, all.Data?.Count() ?? 0);
            var data = all.Data ?? new List<EntityNetGameItem>();
            var q = string.IsNullOrWhiteSpace(keyword) ? data.ToList() : data.Where(s => (s.Name ?? string.Empty).IndexOf(keyword!, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            Log.Debug("[SearchServers] 过滤后数量: {FilteredCount}", q.Count);
            var pageItems = q.Skip(offset).Take(pageSize).Select(s => new ServerItem { EntityId = s.EntityId, Name = s.Name }).ToList();
            var hasMore = offset + pageSize < q.Count;
            Log.Debug("[SearchServers] 返回页面数量: {PageCount}, hasMore={HasMore}", pageItems.Count, hasMore);
            return new ListServersResult { Success = true, Items = pageItems, HasMore = hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new ListServersResult { Success = false, Message = "获取失败" };
        }
    }
}
