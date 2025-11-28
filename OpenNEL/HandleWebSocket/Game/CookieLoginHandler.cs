using Codexus.OpenSDK;
using OpenNEL.type;
using OpenNEL.network;

using System.Text.Json;
using Serilog;

namespace OpenNEL.HandleWebSocket.Game;

internal class CookieLoginHandler : IWsHandler
{
    public string Type => "cookie_login";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var cookie = root.TryGetProperty("cookie", out var c) ? c.GetString() : null;
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return new { type = "login_error", message = "cookie为空" };
        }
        try
        {
            if (AppState.Services == null || AppState.Services.X19 == null)
            {
                return new { type = "login_error", message = "系统未初始化" };
            }
            await AppState.Services.X19.InitializeDeviceAsync();
            var cont = await AppState.Services.X19.ContinueAsync(cookie);
            var authOtp = cont.Item1;
            var channel = cont.Item2;
            await X19.InterconnectionApi.LoginStart(authOtp.EntityId, authOtp.Token);
            Log.Information("Cookie登录成功: {Id}, {Channel}", authOtp.EntityId, channel);
            AppState.Accounts[authOtp.EntityId] = channel;
            AppState.Auths[authOtp.EntityId] = authOtp;
            AppState.SelectedAccountId = authOtp.EntityId;
            return new { type = "Success_login", entityId = authOtp.EntityId, channel };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Cookie登录失败");
            return new { type = "login_error", message = "登录失败" };
        }
    }
}
