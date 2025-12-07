using System.Linq;
using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Protocol;
using OpenNEL.Entities.Web;
using OpenNEL.Manager;
using OpenNEL.type;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class LoginX19
    {
        public object Execute(string email, string password)
        {
            try
            {
                WPFLauncher x = AppState.X19;
                EntityX19CookieRequest req = WPFLauncher.GenerateCookie(x.LoginWithEmailAsync(email ?? string.Empty, password ?? string.Empty).GetAwaiter().GetResult(), x.MPay.GetDevice());
                var (authOtp, channel) = x.LoginWithCookie(req);
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "cookie",
                    Details = JsonSerializer.Serialize(req)
                });
                var list = new System.Collections.ArrayList();
                list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
                var users = UserManager.Instance.GetUsersNoDetails();
                var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (System.Exception ex)
            {
                return new { type = "login_error", message = ex.Message ?? "登录失败" };
            }
        }
    }
}
