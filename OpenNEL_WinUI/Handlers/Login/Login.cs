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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using OpenNEL.MPay.Entities;
using OpenNEL.MPay.Exceptions;
using OpenNEL.WPFLauncher.Entities;
using OpenNEL.Pc4399;
using OpenNEL_WinUI.Entities;
using OpenNEL_WinUI.Entities.Web;
using OpenNEL_WinUI.Entities.Web.NEL;
using OpenNEL_WinUI.Enums;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login
{
    public static class LoginHandler
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static void LoginWithChannelAndType(string channel, string type, string details, Platform platform, string token)
        {
            LoginWith4399Password(details, platform, token);
        }
        
        private static void LoginWith4399Password(string details, Platform platform, string token)
        {
            EntityPasswordRequest? entityPasswordRequest = JsonSerializer.Deserialize<EntityPasswordRequest>(details);
            if (entityPasswordRequest == null)
            {
                throw new ArgumentException("Invalid password login details");
            }
            using Pc4399Client pc = new Pc4399Client();
            string result2;
            try
            {
                result2 = pc.LoginWithPasswordAsync(entityPasswordRequest.Account, entityPasswordRequest.Password, entityPasswordRequest.CaptchaIdentifier, entityPasswordRequest.Captcha).GetAwaiter().GetResult();
            }
            catch (CaptchaException)
            {
                throw new CaptchaException("captcha required");
            }
            if (string.IsNullOrWhiteSpace(result2))
            {
                throw new Exception("cookie empty");
            }
            EntityX19CookieRequest cookieReq;
            try
            {
                cookieReq = JsonSerializer.Deserialize<EntityX19CookieRequest>(result2) ?? new EntityX19CookieRequest { Json = result2 };
            }
            catch
            {
                cookieReq = new EntityX19CookieRequest { Json = result2 };
            }
            var (entityAuthenticationOtp2, text) = AppState.X19.LoginWithCookie(cookieReq);
            Log.Information("Login with password: {UserId} Channel: {LoginChannel}", entityAuthenticationOtp2.EntityId, text);
            Log.Debug("User details: {UserId},{Token}", entityAuthenticationOtp2.EntityId, entityAuthenticationOtp2.Token);
            UserManager.Instance.AddUserToMaintain(entityAuthenticationOtp2);
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = entityAuthenticationOtp2.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = text,
                Type = "password",
                Details = JsonSerializer.Serialize(new EntityPasswordRequest
                {
                    Account = entityPasswordRequest.Account,
                    Password = entityPasswordRequest.Password
                }, DefaultOptions)
            });
        }
    }
}
