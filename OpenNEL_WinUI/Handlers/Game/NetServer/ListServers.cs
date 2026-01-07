/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Linq;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using OpenNEL_WinUI.Utils;
using Serilog;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Entities.Web.NetGame;

namespace OpenNEL_WinUI.Handlers.Game.NetServer;

public class ListServers
{
    public ListServersResult Execute(int offset, int pageSize)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new ListServersResult { NotLogin = true };
        try
        {
            var servers = AppState.X19.GetAvailableNetGames(last.UserId, last.AccessToken, offset, pageSize);
            if (AppState.Debug) Log.Information("服务器列表: 数量={Count}", servers.Data?.Length ?? 0);
            var data = servers.Data?.ToList() ?? new System.Collections.Generic.List<EntityNetGameItem>();
            var items = data.Select(s => new ServerItem { EntityId = s.EntityId, Name = s.Name }).ToList();
            return new ListServersResult { Success = true, Items = items, HasMore = data.Count >= pageSize };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取服务器列表失败");
            return new ListServersResult { Success = false, Message = "获取失败" };
        }
    }
}
