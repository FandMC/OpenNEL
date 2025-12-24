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
using OpenNEL.MPay.Exceptions;
using OpenNEL.WPFLauncher.Entities;
using OpenNEL_WinUI.Entities.Web.NEL;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class Login4399
    {
        public object Execute(string account, string password, string sessionId = null, string captcha = null)
        {
            try
            {
                AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();
                var c4399 = new Codexus.OpenSDK.C4399();
                string cookieJson = (!string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(captcha))
                    ? c4399.LoginWithPasswordAsync(account, password, sessionId, captcha).GetAwaiter().GetResult()
                    : c4399.LoginWithPasswordAsync(account, password).GetAwaiter().GetResult();
                if (AppState.Debug) Log.Information("4399 Login cookieJson length: {Length}", cookieJson.Length);
                if (string.IsNullOrWhiteSpace(cookieJson))
                {
                    var err = new { type = "login_4399_error", message = "cookie empty" };
                    return err;
                }
                EntityX19CookieRequest cookieReq;
                
                cookieReq = new EntityX19CookieRequest { Json = cookieJson };
                
                var (authOtp, channel) = AppState.X19.LoginWithCookie(cookieReq);
                if (AppState.Debug) Log.Information("X19 LoginWithCookie: {UserId} Channel: {Channel}", authOtp.EntityId, channel);
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new OpenNEL_WinUI.Entities.Web.EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "password",
                    Details = JsonSerializer.Serialize(new EntityPasswordRequest { Account = account ?? string.Empty, Password = password ?? string.Empty })
                });
                var list = new System.Collections.ArrayList();
                list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (CaptchaException ce)
            {
                if (AppState.Debug) Log.Error(ce, "WS 4399 captcha required. account={Account}", account ?? string.Empty);
                var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
                var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;
                var msg = new { type = "captcha_required", account, password, sessionId = captchaSid, captchaUrl = url };
                return msg;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var lower = msg.ToLowerInvariant();
                if (AppState.Debug) Log.Error(ex, "WS 4399 login exception. account={Account} sid={Sid}", account ?? string.Empty, sessionId ?? string.Empty);
                if (lower.Contains("parameter") && lower.Contains("'s'"))
                {
                    var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
                    var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;
                    var r = new { type = "captcha_required", account, password, sessionId = captchaSid, captchaUrl = url };
                    return r;
                }
                var err = new { type = "login_4399_error", message = msg.Length == 0 ? "登录失败" : msg };
                return err;
            }
        }
    }
}
