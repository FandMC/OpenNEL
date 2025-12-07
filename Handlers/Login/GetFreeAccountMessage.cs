using System;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class GetFreeAccount
    {
        public async Task<object[]> Execute(string apiBase = null,
                                            int timeoutSec = 30,
                                            string userAgent = null,
                                            int maxRetries = 3,
                                            bool ignoreSslErrors = false,
                                            string username = null,
                                            string password = null,
                                            string idcard = null,
                                            string realname = null,
                                            string captchaId = null,
                                            string captcha = null)
        {
            Log.Information("正在获取4399小号...");
            var status = new { type = "get_free_account_status", status = "processing", message = "获取小号中, 这可能需要点时间..." };
            HttpClient? client = null;
            object? resultPayload = null;
            try
            {
                var apiBaseEnv = Environment.GetEnvironmentVariable("SAMSE_API_BASE");
                var baseUrl = string.IsNullOrWhiteSpace(apiBaseEnv) ? (string.IsNullOrWhiteSpace(apiBase) ? "http://4399.11pw.pw" : apiBase) : apiBaseEnv;
                var ua = string.IsNullOrWhiteSpace(userAgent) ? "Samse-4399-Client/1.0" : userAgent;
                var allowInsecure = ignoreSslErrors || string.Equals(Environment.GetEnvironmentVariable("SAMSE_IGNORE_SSL"), "1", StringComparison.OrdinalIgnoreCase);
                var handler = new HttpClientHandler();
                handler.AutomaticDecompression = DecompressionMethods.All;
                if (allowInsecure)
                {
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeoutSec) };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
                var url = baseUrl.TrimEnd('/') + "/reg4399";
                var payload = new System.Collections.Generic.Dictionary<string, object?>();
                if (!string.IsNullOrEmpty(username)) payload["username"] = username;
                if (!string.IsNullOrEmpty(password)) payload["password"] = password;
                if (!string.IsNullOrEmpty(idcard)) payload["idcard"] = idcard;
                if (!string.IsNullOrEmpty(realname)) payload["realname"] = realname;
                if (!string.IsNullOrEmpty(captchaId)) payload["captchaId"] = captchaId;
                if (!string.IsNullOrEmpty(captcha)) payload["captcha"] = captcha;
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
                        var u = TryGetString(d, "username") ?? "";
                        var p = TryGetString(d, "password") ?? "";
                        var ck = TryGetString(d, "cookie");
                        var ckErr = TryGetString(d, "cookieError");
                        Log.Information("获取成功: {Username} {Password}", u, p);
                        resultPayload = new
                        {
                            type = "get_free_account_result",
                            success = true,
                            username = u,
                            password = p,
                            cookie = ck,
                            cookieError = ckErr,
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
    }
}
