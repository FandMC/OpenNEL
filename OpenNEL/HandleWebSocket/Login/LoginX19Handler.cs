using OpenNEL.network;
using OpenNEL.type;
using System.Text.Json;
using Codexus.OpenSDK;

namespace OpenNEL.HandleWebSocket.Login;

internal class LoginX19Handler : IWsHandler
{
    public string Type => "login_x19";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        var password = root.TryGetProperty("password", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new { type = "login_error", message = "邮箱或密码为空" };
        }
        try
        {
            var mpay = new UniSdkMPay(Projects.DesktopMinecraft, "2.1.0");
            await mpay.InitializeDeviceAsync();
            var user = await mpay.LoginWithEmailAsync(email, password);
            if (user == null)
            {
                return new { type = "login_error", message = "MPay登录失败" };
            }
            var x19 = new X19();
            var result = await x19.ContinueAsync(user, mpay.Device);
            var authOtp = result.Item1;
            var channel = result.Item2;
            await X19.InterconnectionApi.LoginStart(authOtp.EntityId, authOtp.Token);
            AppState.Accounts[authOtp.EntityId] = channel;
            AppState.Auths[authOtp.EntityId] = authOtp;
            AppState.SelectedAccountId = authOtp.EntityId;
            return new { type = "Success_login", entityId = authOtp.EntityId, channel };
        }
        catch (System.Exception ex)
        {
            return new { type = "login_error", message = ex.Message };
        }
    }
}
