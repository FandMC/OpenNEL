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
using OpenNEL_WinUI.Entities.Web;
using OpenNEL_WinUI.Manager;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class ActivateAccount
    {
        public object Execute(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return new { type = "activate_account_error", message = "缺少id" };
            var u = UserManager.Instance.GetUserByEntityId(id!);
            if (u == null) return new { type = "activate_account_error", message = "账号不存在" };
            try
            {
                if (!u.Authorized)
                {
                    LoginHandler.LoginWithChannelAndType(u.Channel, u.Type, u.Details, u.Platform, string.Empty);
                }
                var list = new System.Collections.ArrayList();
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (OpenNEL.MPay.Exceptions.CaptchaException)
            {
                try
                {
                    var req = JsonSerializer.Deserialize<Entities.Web.NEL.EntityPasswordRequest>(u.Details);
                    var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
                    var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;
                    return new { type = "captcha_required", account = req?.Account ?? string.Empty, password = req?.Password ?? string.Empty, sessionId = captchaSid, captchaUrl = url };
                }
                catch
                {
                    return new { type = "captcha_required" };
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var lower = msg.ToLowerInvariant();
                if (lower.Contains("parameter") && lower.Contains("'s'"))
                {
                    try
                    {
                        var req = JsonSerializer.Deserialize<Entities.Web.NEL.EntityPasswordRequest>(u.Details);
                        var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
                        var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;
                        return new { type = "captcha_required", account = req?.Account ?? string.Empty, password = req?.Password ?? string.Empty, sessionId = captchaSid, captchaUrl = url };
                    }
                    catch
                    {
                        return new { type = "captcha_required" };
                    }
                }
                return new { type = "activate_account_error", message = msg.Length == 0 ? "激活失败" : msg };
            }
        }
    }
}
