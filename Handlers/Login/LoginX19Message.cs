using System.Linq;
using Codexus.Cipher.Entities.WPFLauncher;
using OpenNEL.Entities.Web;
using OpenNEL.Manager;
using OpenNEL.type;
using Codexus.OpenSDK;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class LoginX19
    {
        public object Execute(string email, string password)
        {
            try
            {
                var mpay = new UniSdkMPay(Projects.DesktopMinecraft, "2.1.0");
                mpay.InitializeDeviceAsync().GetAwaiter().GetResult();
                var user = mpay.LoginWithEmailAsync(email, password).GetAwaiter().GetResult();
                if (user == null)
                {
                    return new { type = "login_error", message = "MPay登录失败" };
                }
                var x19sdk = AppState.Services != null ? AppState.Services.X19 : new X19();
                var result = x19sdk.ContinueAsync(user, mpay.Device).GetAwaiter().GetResult();
                var openAuth = result.Item1;
                var channel = result.Item2;
                try { X19.InterconnectionApi.LoginStart(openAuth.EntityId, openAuth.Token).GetAwaiter().GetResult(); } catch { }
                var authOtp = new EntityAuthenticationOtp { EntityId = openAuth.EntityId, Token = openAuth.Token };
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "cookie",
                    Details = ""
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
                return new { type = "login_error", message = ex.Message};
            }
        }
    }
}
