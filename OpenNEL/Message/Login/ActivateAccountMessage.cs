using System.Text.Json;
using OpenNEL.Entities.Web;
using OpenNEL.Manager;
using OpenNEL.Network;

namespace OpenNEL.Message.Login;

internal class ActivateAccountMessage : IWsMessage
{
    public string Type => "activate_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var idp) ? idp.GetString() : (root.TryGetProperty("entityId", out var idp2) ? idp2.GetString() : null);
        if (string.IsNullOrWhiteSpace(id)) return new { type = "activate_account_error", message = "缺少id" };
        var u = UserManager.Instance.GetUserByEntityId(id!);
        if (u == null) return new { type = "activate_account_error", message = "账号不存在" };
        try
        {
            if (!u.Authorized)
            {
                LoginMessage.LoginWithChannelAndType(u.Channel, u.Type, u.Details, u.Platform, string.Empty);
            }
            var list = new System.Collections.ArrayList();
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(x => new { entityId = x.UserId, channel = x.Channel, status = x.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "Success_login", entityId = u.UserId, channel = u.Channel });
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (Codexus.Cipher.Utils.Exception.CaptchaException)
        {
            try
            {
                var req = JsonSerializer.Deserialize<OpenNEL.Entities.Web.NEL.EntityPasswordRequest>(u.Details);
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
                    var req = JsonSerializer.Deserialize<OpenNEL.Entities.Web.NEL.EntityPasswordRequest>(u.Details);
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
