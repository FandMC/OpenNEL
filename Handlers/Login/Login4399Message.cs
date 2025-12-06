using System;
using System.Linq;
using System.Text.Json;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Exception;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.Manager;
using Serilog;
using OpenNEL.type;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class Login4399
    {
        public object Execute(string account, string password, string sessionId = null, string captcha = null)
        {
            try
            {
                OpenNEL.type.AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();
                string cookieJson = (!string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(captcha))
                    ? OpenNEL.type.AppState.Services!.C4399.LoginWithPasswordAsync(account ?? string.Empty, password ?? string.Empty, sessionId!, captcha!).GetAwaiter().GetResult()
                    : OpenNEL.type.AppState.Services!.C4399.LoginWithPasswordAsync(account ?? string.Empty, password ?? string.Empty).GetAwaiter().GetResult();
                if (AppState.Debug) Log.Information("4399 Login cookieJson length: {Length}", cookieJson?.Length ?? 0);
                if (string.IsNullOrWhiteSpace(cookieJson))
                {
                    var err = new { type = "login_4399_error", message = "cookie empty" };
                    return err;
                }
                Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest cookieReq;
                try
                {
                    cookieReq = JsonSerializer.Deserialize<Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest>(cookieJson) ?? new Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest { Json = cookieJson };
                }
                catch (Exception de)
                {
                    if (AppState.Debug) Log.Error(de, "Deserialize cookieJson failed: length={Length}", cookieJson?.Length ?? 0);
                    cookieReq = new Codexus.Cipher.Entities.WPFLauncher.EntityX19CookieRequest { Json = cookieJson };
                }
                var (authOtp, channel) = OpenNEL.type.AppState.X19.LoginWithCookie(cookieReq);
                if (AppState.Debug) Log.Information("X19 LoginWithCookie: {UserId} Channel: {Channel}", authOtp.EntityId, channel);
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new OpenNEL.Entities.Web.EntityUser
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
                var users = UserManager.Instance.GetUsersNoDetails();
                var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
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
            catch (System.Exception ex)
            {
                var msg = ex.Message ?? string.Empty;
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
