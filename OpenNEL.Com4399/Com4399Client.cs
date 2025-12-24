using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenNEL.Com4399.Entities;
using OpenNEL.Core.Http;
using OpenNEL.MPay;

namespace OpenNEL.Com4399;

public partial class Com4399Client
{
    private const string DeviceFileName = "4399com.cds";

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpWrapper _4399Api = new("https://m.4399api.com");
    private readonly Lock _lock = new();
    private readonly HttpWrapper _login = new("https://ptlogin.4399.com", null, new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

    private string _deviceIdentifier = string.Empty;
    private string _deviceIdentifierSm = string.Empty;
    private string _state = string.Empty;
    private string _udid = string.Empty;

    public Com4399Client()
    {
        CreateOrLoadDevice().GetAwaiter().GetResult();
    }

    private async Task CreateOrLoadDevice()
    {
        var data = MPayClient.LoadFromFile(DeviceFileName);
        var device = data != null ? LoadDevice(data) : await CreateDevice();
        if (device.DeviceState == null)
        {
            device = await CreateDevice();
        }
        _deviceIdentifier = device.DeviceIdentifier;
        _deviceIdentifierSm = device.DeviceIdentifierSm;
        _udid = device.DeviceUdid;
        _state = device.DeviceState!;
    }

    private static Entity4399Device LoadDevice(byte[] data)
    {
        return JsonSerializer.Deserialize<Entity4399Device>(data)!;
    }

    private async Task<Entity4399Device> CreateDevice()
    {
        _deviceIdentifier = GenerateIdentifier();
        _deviceIdentifierSm = GenerateIdentifier();
        _udid = Guid.NewGuid().ToString();
        var deviceState = await OAuthDevice();
        var device = new Entity4399Device
        {
            DeviceIdentifier = _deviceIdentifier,
            DeviceIdentifierSm = _deviceIdentifierSm,
            DeviceUdid = _udid,
            DeviceState = deviceState
        };
        lock (_lock)
        {
            MPayClient.SaveToFile(DeviceFileName, JsonSerializer.Serialize(device, DefaultOptions));
            return device;
        }
    }

    private async Task<string> OAuthDevice()
    {
        var body = new ParameterBuilder()
            .Append("usernames", "")
            .Append("top_bar", "1")
            .Append("state", "")
            .Append("device", JsonSerializer.Serialize(new Entity4399OAuth
            {
                DeviceIdentifier = _deviceIdentifier,
                DeviceIdentifierSm = _deviceIdentifierSm,
                Udid = _udid
            }, DefaultOptions))
            .FormUrlEncode();
        var response = await _4399Api.PostAsync("/openapiv2/oauth.html", body, "application/x-www-form-urlencoded");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var oauthResponse = JsonSerializer.Deserialize<Entity4399OAuthResponse>(content)
            ?? throw new Exception("Failed to deserialize: " + content);
        return new ParameterBuilder(oauthResponse.Result.LoginUrl).Get("state");
    }

    public async Task<string> LoginAndAuthorize(string username, string password, string? captcha = null, string? captchaId = null, int retry = 0)
    {
        if (retry > 5)
        {
            throw new Exception("Retry limit exceeded");
        }

        var builder = new ParameterBuilder()
            .Append("_d", _deviceIdentifier)
            .Append("access_token", "")
            .Append("aid", "")
            .Append("auth_action", "ORILOGIN")
            .Append("auto_scroll", "")
            .Append("autoCreateAccount", "")
            .Append("bizId", "2100001792")
            .Append("cid", "")
            .Append("client_id", "40f9e9b95d6c71ba5c6e0bd14c0abeff")
            .Append("css", "")
            .Append("expand_ext_login_list", "")
            .Append("game_key", "115716")
            .Append("isInputRealname", "false")
            .Append("isValidRealname", "false")
            .Append("password", password)
            .Append("redirect_uri", "https://m.4399api.com/openapi/oauth-callback.html?gamekey=44770")
            .Append("ref", "{\"game\":\"115716\",\"channel\":\"\"}")
            .Append("reg_mode", "reg_phone")
            .Append("response_type", "TOKEN")
            .Append("scope", "basic")
            .Append("sec", "1")
            .Append("show_4399", "")
            .Append("show_back_button", "")
            .Append("show_close_button", "")
            .Append("show_ext_login", "")
            .Append("show_forget_password", "")
            .Append("show_topbar", "false")
            .Append("state", _state)
            .Append("uid", "")
            .Append("username_history", "")
            .Append("username", username.ToLowerInvariant());

        if (captcha != null && captchaId != null)
        {
            builder.Append("captcha", captcha).Append("captcha_id", captchaId);
        }

        var body = builder.FormUrlEncode();
        var redirect = await _login.PostAsync("/oauth2/loginAndAuthorize.do?channel=", body, "application/x-www-form-urlencoded");
        var content = await redirect.Content.ReadAsStringAsync();

        if (content.Contains("验证码"))
        {
            return await HandleCaptchaWithHtml(username, password, content, retry);
        }

        var redirectBuilder = new ParameterBuilder(redirect.Headers.Location!.AbsoluteUri);
        if (captcha != null && captchaId == null)
        {
            redirectBuilder.Append("captcha", captcha);
        }

        var callbackResponse = await new HttpWrapper(redirectBuilder.ToQueryUrl()).GetAsync("");
        callbackResponse.EnsureSuccessStatusCode();
        var callbackContent = await callbackResponse.Content.ReadAsStringAsync();

        if (callbackContent.Contains("登录状态已失效，请重新登录"))
        {
            var newState = await OAuthDevice();
            var device = new Entity4399Device
            {
                DeviceIdentifier = _deviceIdentifier,
                DeviceIdentifierSm = _deviceIdentifierSm,
                DeviceUdid = _udid,
                DeviceState = newState
            };
            _state = newState;
            lock (_lock)
            {
                MPayClient.SaveToFile(DeviceFileName, JsonSerializer.Serialize(device, DefaultOptions));
            }
            return await LoginAndAuthorize(username, password);
        }

        if (callbackContent.Contains("登录成功，但账号存在异常，需要验证"))
        {
            throw new Exception("Account requires verification: " + callbackContent);
        }

        var userInfoResponse = JsonSerializer.Deserialize<Entity4399UserInfoResponse>(callbackContent);
        if (userInfoResponse == null || userInfoResponse.Code != "100")
        {
            throw new Exception("Failed to deserialize: " + callbackContent);
        }

        return GenerateSAuth(userInfoResponse.Result);
    }

    private string GenerateSAuth(Entity4399UserInfoResult result)
    {
        var deviceId = Guid.NewGuid().ToString("N").ToUpper();
        var cookie = new EntityMgbSdkCookie
        {
            Ip = "127.0.0.1",
            AimInfo = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}",
            AppChannel = "4399com",
            ClientLoginSn = deviceId,
            DeviceId = deviceId,
            GameId = "x19",
            LoginChannel = "4399com",
            SdkUid = result.Uid.ToString(),
            SessionId = result.State.ToUpper(),
            Timestamp = "",
            Platform = "ad",
            SourcePlatform = "ad",
            Udid = deviceId,
            UserId = ""
        };
        return JsonSerializer.Serialize(cookie, DefaultOptions);
    }

    private async Task<string> HandleCaptchaWithHtml(string username, string password, string html, int retry)
    {
        var match = CaptchaRegex().Match(html);
        if (!match.Success)
        {
            throw new Exception("Cannot find captcha in html");
        }
        var captchaId = match.Groups[1].Value;
        // Note: 在实际使用中需要实现验证码识别逻辑
        throw new Exception($"Captcha required, captcha_id: {captchaId}. Please implement captcha handling.");
    }

    private static string GenerateIdentifier(DateTime? dateTime = null, string? additionalData = null)
    {
        var time = (dateTime ?? DateTime.Now).ToString("yyyyMMddHHmm");
        var hash = GenerateHash50(additionalData);
        return time + hash;
    }

    private static string GenerateHash50(string? data = null)
    {
        if (string.IsNullOrEmpty(data))
        {
            data = Guid.NewGuid().ToString() + DateTime.Now.Ticks;
        }
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(data)))[..50];
    }

    [GeneratedRegex(@"name\s*=\s*[""']captcha_id[""']\s+value\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex CaptchaRegex();
}
