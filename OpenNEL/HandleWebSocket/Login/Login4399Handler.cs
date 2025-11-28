using Codexus.OpenSDK;
using OpenNEL.network;
using OpenNEL.type;
using OpenNEL.Utils;
using System.Text.Json;
using Serilog;
using Codexus.OpenSDK.Exceptions;

namespace OpenNEL.HandleWebSocket.Login;

internal class Login4399Handler : IWsHandler
{
    public string Type => "login_4399";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        string? loginJson = null;
        var req = Parse(root);
        if (string.IsNullOrWhiteSpace(req.account) || string.IsNullOrWhiteSpace(req.password))
        {
            return new { type = "login_error", message = "账号或密码为空" };
        }
        try
        {
            await AppState.Services!.X19.InitializeDeviceAsync();
            var cookieJson = await AppState.Services!.C4399.LoginWithPasswordAsync(req.account,req.password);
            var cont = await AppState.Services.X19.ContinueAsync(cookieJson);
            var otp = cont.Item1;
            var channel = cont.Item2;
            AppState.Accounts[otp.EntityId] = channel;
            AppState.Auths[otp.EntityId] = otp;
            AppState.SelectedAccountId = otp.EntityId;
            return new { type = "Success_login", entityId = otp.EntityId, channel };
        }
        catch (Exception ex) when (ex.Data.Contains("captcha_url") && ex.Data.Contains("session_id"))
        {
            var capUrl = ex.Data["captcha_url"]?.ToString();
            var sidVal = ex.Data["session_id"]?.ToString();
            if (Debug.Get()) Log.Information("login_4399 captcha_required ex: {Message} data: {Data}", ex.Message, DumpData(ex.Data));
            return new { type = "captcha_required", account = req.account, password = req.password, captchaUrl = capUrl, sessionId = sidVal };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "4399登录失败");
            if (Debug.Get()) Log.Information("login_4399 exception data: {Data}", DumpData(ex.Data));
            return new { type = "login_error", message = ex.Message ?? "登录失败" };
        }
    }

    private static (string account, string password, string? sessionId, string? captcha) Parse(JsonElement root)
    {
        var account = root.TryGetProperty("account", out var acc) ? acc.GetString() : null;
        var password = root.TryGetProperty("password", out var pwd) ? pwd.GetString() : null;
        var sessionId = root.TryGetProperty("sessionId", out var sid) ? sid.GetString() : null;
        var captcha = root.TryGetProperty("captcha", out var cap) ? cap.GetString() : null;
        return (account ?? string.Empty, password ?? string.Empty, sessionId, captcha);
    }

    private static async Task<string> LoginAsync((string account, string password, string? sessionId, string? captcha) req)
    {
        if (!string.IsNullOrWhiteSpace(req.sessionId) && !string.IsNullOrWhiteSpace(req.captcha))
        {
            return await AppState.Services!.C4399.LoginWithPasswordAsync(req.account, req.password, req.sessionId, req.captcha);
        }
        return await AppState.Services!.C4399.LoginWithPasswordAsync(req.account, req.password);
    }

    

    private static string DumpData(System.Collections.IDictionary data)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            foreach (var k in data.Keys)
            {
                var v = data[k];
                sb.Append(k).Append('=').Append(v).Append(';');
            }
            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
}
