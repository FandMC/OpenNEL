using OpenNEL.network;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net;
using Serilog;

namespace OpenNEL.HandleWebSocket.Login;

internal class GetFreeAccountHandler : IWsHandler
{
    public string Type => "get_free_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        Log.Information("正在获取4399小号...");
        var status = new { type = "get_free_account_status", status = "processing", message = "获取小号中, 这可能需要点时间..." };
        HttpClient? client = null;
        object? resultPayload = null;
        try
        {
            var apiBaseEnv = Environment.GetEnvironmentVariable("SAMSE_API_BASE");
            var apiBaseReq = TryGetString(root, "apiBase");
            var apiBase = string.IsNullOrWhiteSpace(apiBaseEnv) ? (string.IsNullOrWhiteSpace(apiBaseReq) ? "http://4399.11pw.pw" : apiBaseReq) : apiBaseEnv;
            var timeoutSec = TryGetInt(root, "timeout", 30);
            var userAgent = TryGetString(root, "userAgent") ?? "Samse-4399-Client/1.0";
            var maxRetries = TryGetInt(root, "maxRetries", 3);
            var allowInsecure = TryGetBool(root, "ignoreSslErrors") || string.Equals(Environment.GetEnvironmentVariable("SAMSE_IGNORE_SSL"), "1", StringComparison.OrdinalIgnoreCase);
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.All;
            if (allowInsecure)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeoutSec) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            var url = apiBase.TrimEnd('/') + "/reg4399";
            var payload = new System.Collections.Generic.Dictionary<string, object?>();
            AddIfPresent(payload, root, "username");
            AddIfPresent(payload, root, "password");
            AddIfPresent(payload, root, "idcard");
            AddIfPresent(payload, root, "realname");
            AddIfPresent(payload, root, "captchaId");
            AddIfPresent(payload, root, "captcha");
            HttpResponseMessage? resp = null;
            for (var attempt = 0; attempt < Math.Max(1, maxRetries); attempt++)
            {
                try
                {
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    resp = await client.PostAsync(url, content);
                    break;
                }
                catch when (attempt < Math.Max(1, maxRetries) - 1)
                {
                    await Task.Delay(1000);
                }
            }
            if (resp == null)
            {
                resultPayload = new { type = "get_free_account_result", success = false, message = "网络错误" };
            }
            else
            {
                var body = await resp.Content.ReadAsStringAsync();
                JsonElement d;
                try
                {
                    d = JsonDocument.Parse(body).RootElement;
                }
                catch (Exception)
                {
                    resultPayload = new { type = "get_free_account_result", success = false, message = "响应解析失败" };
                    goto End;
                }
                var success = d.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;
                if (success)
                {
                    var username = TryGetString(d, "username") ?? "";
                    var password = TryGetString(d, "password") ?? "";
                    var cookie = TryGetString(d, "cookie");
                    var cookieError = TryGetString(d, "cookieError");
                    Log.Information("获取成功: {Username} {Password}", username, password);
                    resultPayload = new
                    {
                        type = "get_free_account_result",
                        success = true,
                        username = username,
                        password = password,
                        cookie = cookie,
                        cookieError = cookieError,
                        message = "获取成功！"
                    };
                }
                else
                {
                    var requiresCaptcha = d.TryGetProperty("requiresCaptcha", out var rc) && rc.ValueKind == JsonValueKind.True;
                    if (requiresCaptcha)
                    {
                        resultPayload = new
                        {
                            type = "get_free_account_requires_captcha",
                            requiresCaptcha = true,
                            captchaId = TryGetString(d, "captchaId"),
                            captchaImageUrl = TryGetString(d, "captchaImageUrl"),
                            username = TryGetString(d, "username"),
                            password = TryGetString(d, "password"),
                            idcard = TryGetString(d, "idcard"),
                            realname = TryGetString(d, "realname")
                        };
                    }
                    else
                    {
                        resultPayload = new { type = "get_free_account_result", success = false, message = body };
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "错误: {Message}", e.Message);
            resultPayload = new { type = "get_free_account_result", success = false, message = "错误: " + e.Message };
        }
        finally
        {
            client?.Dispose();
        }
        End:
        return new object[] { status, resultPayload ?? new { type = "get_free_account_result", success = false, message = "未知错误" } };
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            if (v.ValueKind == JsonValueKind.Number) return v.ToString();
            if (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) return v.ToString();
        }
        return null;
    }

    private static int TryGetInt(JsonElement root, string name, int def)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number)
            {
                if (v.TryGetInt32(out var n)) return n;
            }
            if (v.ValueKind == JsonValueKind.String)
            {
                if (int.TryParse(v.GetString(), out var n)) return n;
            }
        }
        return def;
    }

    private static void AddIfPresent(System.Collections.Generic.Dictionary<string, object?> dict, JsonElement root, string name)
    {
        var val = TryGetString(root, name);
        if (!string.IsNullOrEmpty(val)) dict[name] = val;
    }

    private static bool TryGetBool(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
            if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }
}
