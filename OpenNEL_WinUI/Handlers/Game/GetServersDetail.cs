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
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;
using System.Text.Json;

namespace OpenNEL_WinUI.Handlers.Game
{
    public class GetServersDetail
    {
        public object Execute(string gameId)
        {
            var last = UserManager.Instance.GetLastAvailableUser();
            if (last == null) return new { type = "notlogin" };
            if (string.IsNullOrWhiteSpace(gameId)) return new { type = "server_detail_error", message = "参数错误" };
            try
            {
                var detail = AppState.X19.QueryNetGameDetailById(last.UserId, last.AccessToken, gameId);
                var dataProp = detail?.GetType().GetProperty("Data");
                var dataVal = dataProp != null ? dataProp.GetValue(detail) : null;
                var imgs = new List<string>();
                if (dataVal != null)
                {
                    var upProp = dataVal.GetType().GetProperty("BriefImageUrls");
                    var lowProp = dataVal.GetType().GetProperty("brief_image_urls");
                    var arr = upProp != null ? upProp.GetValue(dataVal) as System.Collections.IEnumerable : null;
                    if (arr == null && lowProp != null) arr = lowProp.GetValue(dataVal) as System.Collections.IEnumerable;
                    if (arr != null)
                    {
                        foreach (var it in arr)
                        {
                            var s = it != null ? it.ToString() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(s)) imgs.Add(s.Replace("`", string.Empty).Trim());
                        }
                    }
                }
                return new { type = "server_detail", images = imgs.ToArray() };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取服务器详情失败: {GameId}", gameId);
                return new { type = "server_detail_error", message = "获取失败" };
            }
        }
    }
}
