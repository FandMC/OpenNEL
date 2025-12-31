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
using System.Collections;
using System.Text.Json;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Exception;
using OpenNEL_WinUI.Entities.Web;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login;

public class LoginX19
{
    public object Execute(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new { type = "login_x19_error", message = "邮箱或密码不能为空" };
            }

            AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();

            var wpf = AppState.X19;
            var mPayUser = wpf.LoginWithEmailAsync(email, password).GetAwaiter().GetResult();
            var device = wpf.MPay.GetDevice();
            var cookieRequest = WPFLauncher.GenerateCookie(mPayUser, device);
            var (authOtp, channel) = wpf.LoginWithCookie(cookieRequest);

            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = authOtp.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = channel,
                Type = "netease",
                Details = JsonSerializer.Serialize(new { email, password })
            });

            var list = new ArrayList();
            list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            var items = GetAccount.GetAccountItems();
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (VerifyException ve)
        {
            Log.Error(ve, "[LoginX19] 验证失败");
            return new { type = "login_x19_error", message = ve.Message};
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LoginX19] 登录异常");
            var msg = ex.Message;
            if (msg.Contains("password") || msg.Contains("密码"))
            {
                return new { type = "login_x19_error", message = "邮箱或密码错误" };
            }
            return new { type = "login_x19_error", message = msg.Length == 0 ? "登录失败" : msg };
        }
    }
}
