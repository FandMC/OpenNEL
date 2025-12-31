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
using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Entities;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class LoginCookie
    {
        public object Execute(string cookie)
        {
            try
            {
                EntityX19CookieRequest req;
                try
                {
                    req = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookie ?? string.Empty) ?? new EntityX19CookieRequest { Json = cookie ?? string.Empty };
                }
                catch
                {
                    req = new EntityX19CookieRequest { Json = cookie ?? string.Empty };
                }
                var (authOtp, channel) = OpenNEL_WinUI.type.AppState.X19.LoginWithCookie(req);
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new OpenNEL_WinUI.Entities.Web.EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "cookie",
                    Details = cookie ?? string.Empty
                }, channel == "netease");
                var list = new System.Collections.ArrayList();
                list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (ArgumentNullException)
            {
                return new { type = "login_error", message = "当前cookie过期了" };
            }
            catch (System.Exception ex)
            {
                return new { type = "login_error", message = ex.Message ?? "登录失败" };
            }
        }
    }
}
