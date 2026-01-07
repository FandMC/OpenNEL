/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Linq;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Entities.Web.RentalGame;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Game.RentalServer;

public class ListRentalServers
{
    public ListRentalServersResult Execute(int offset, int limit)
    {
        Log.Debug("[RentalServer] ListRentalServers.Execute: offset={Offset}, limit={Limit}", offset, limit);
        
        var user = UserManager.Instance.GetLastAvailableUser();
        if (user == null)
        {
            Log.Debug("[RentalServer] ListRentalServers: 用户未登录");
            return new ListRentalServersResult { NotLogin = true };
        }
        
        Log.Debug("[RentalServer] ListRentalServers: userId={UserId}", user.UserId);

        try
        {
            var result = AppState.X19.GetRentalGameList(user.UserId, user.AccessToken, offset);
            Log.Debug("[RentalServer] GetRentalGameList result: Code={Code}, Count={Count}", result.Code, result.Data?.Count() ?? 0);
            
            var items = result.Data?.Select(item => new RentalServerItem
            {
                EntityId = item.EntityId,
                Name = string.IsNullOrEmpty(item.ServerName) ? item.Name : item.ServerName,
                PlayerCount = (int)item.PlayerCount,
                HasPassword = item.HasPassword == "1",
                McVersion = item.McVersion
            }).ToList() ?? new();
            
            var hasMore = items.Count >= limit;
            Log.Debug("[RentalServer] ListRentalServers: 找到 {Count} 个服务器, hasMore={HasMore}", items.Count, hasMore);
            return new ListRentalServersResult { Success = true, Items = items, HasMore = hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取租赁服列表失败");
            return new ListRentalServersResult { Success = false, Message = "获取失败" };
        }
    }
}
