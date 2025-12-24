using System.Net;
using System.Text.Json;
using OpenNEL.Core.Http;
using OpenNEL.Core.Utils;
using OpenNEL.MPay;
using OpenNEL.Pc4399.Entities;
using OpenNEL.MPay.Exceptions;
using OpenNEL.WPFLauncher;

namespace OpenNEL.Pc4399;

public class Pc4399Client : IDisposable
{
    private const string AppId = "kid_wdsj";
    private const string GameUrl = "https://cdn.h5wan.4399sj.com/microterminal-h5-frame?game_id=500352";

    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpWrapper _login;
    private readonly MgbSdkClient _mgbSdk = new("x19");
    private readonly HttpWrapper _service = new("https://microgame.5054399.net");

    public MPayClient MPay { get; }

    public Pc4399Client()
    {
        MPay = new MPayClient("aecfrxodyqaaaajp-g-x19", WPFLauncherClient.GetLatestVersionAsync().Result);
        _login = new HttpWrapper("https://ptlogin.4399.com", null, new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true
        });
    }

    public void Dispose()
    {
        MPay.Dispose();
        _mgbSdk.Dispose();
        _login.Dispose();
        _service.Dispose();
        _cookieContainer.GetAllCookies().Clear();
        GC.SuppressFinalize(this);
    }

    public async Task<string> LoginWithPasswordAsync(string username, string password, string? captchaIdentifier = null, string? captcha = null)
    {
        bool noCaptcha = captchaIdentifier == null && captcha == null;
        var parameter = BuildParametersLogin()
            .Append("username", username)
            .Append("password", password);

        if (noCaptcha)
        {
            var response = await (await _login.PostAsync("/ptlogin/loginFrame.do?v=1", 
                parameter.FormUrlEncode(), "application/x-www-form-urlencoded")).Content.ReadAsStringAsync();
            if (response.Contains("账号异常，请输入验证码"))
            {
                throw new CaptchaException("Captcha required");
            }
        }

        if (captchaIdentifier != null && captcha != null)
        {
            parameter.Append("sessionId", captchaIdentifier).Append("inputCaptcha", captcha);
        }

        if (!(await _login.PostAsync("/ptlogin/login.do?v=1", 
            parameter.FormUrlEncode(), "application/x-www-form-urlencoded")).IsSuccessStatusCode)
        {
            throw new Exception("Login to Pc4399 failed");
        }

        return await GenerateCookieAsync();
    }

    private async Task<string> GenerateCookieAsync()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var response = await _login.GetAsync("/ptlogin/checkKidLoginUserCookie.do?" + new ParameterBuilder()
            .Append("appId", AppId)
            .Append("gameUrl", GameUrl)
            .Append("isCrossDomain", "1")
            .Append("nick", "null")
            .Append("onLineStart", "false")
            .Append("ptLogin", "true")
            .Append("rand_time", "$randTime")
            .Append("retUrl", $"https://ptlogin.4399.com/resource/ucenter.html?action=login&appId=kid_wdsj&loginLevel=8&regLevel=8&bizId=2201001794&externalLogin=qq&qrLogin=true&layout=vertical&level=101&css=https://microgame.5054399.net/v2/resource/cssSdk/default/login.css&v=2018_11_26_16&postLoginHandler=redirect&checkLoginUserCookie=true&redirectUrl=http%3A%2F%2Fcdn.h5wan.4399sj.com%2Fmicroterminal-h5-frame%3Fgame_id%3D500352%26rand_time%3D{timestamp}")
            .Append("show", "1")
            .FormUrlEncode());

        if (response.RequestMessage?.RequestUri == null)
        {
            throw new Exception("Login to Pc4399 failed");
        }

        var uri = response.RequestMessage.RequestUri.ToString();
        var queryStart = uri.LastIndexOf('?') + 1;
        var queryString = uri.Substring(queryStart);

        var uniAuth = await GetUniAuthAsync(queryString);
        return _mgbSdk.GenerateSAuth(MPay.Unique, uniAuth.Get("username"), uniAuth.Get("uid"), uniAuth.Get("token"), uniAuth.Get("time"), "4399pc");
    }

    private async Task<ParameterBuilder> GetUniAuthAsync(string parameter)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var callback = $"jQuery1830{StringGenerator.GenerateRandomString(16, true, false, false)}_{timestamp}";
        var response = await _service.GetAsync("/v2/service/sdk/info?" + new ParameterBuilder()
            .Append("callback", callback)
            .Append("queryStr", parameter)
            .FormUrlEncode());

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Get Uni-auth failed");
        }

        var content = await response.Content.ReadAsStringAsync();
        var start = content.IndexOf(callback + "(", StringComparison.Ordinal) + callback.Length + 1;
        var json = content.Substring(start, content.Length - 1 - start);
        var result = JsonSerializer.Deserialize<Entity4399Response>(json) ?? throw new Exception("Get Uni-auth failed");
        return new ParameterBuilder(result.Data.SdkLoginData);
    }

    private static ParameterBuilder BuildParametersLogin()
    {
        return new ParameterBuilder()
            .Append("appId", AppId)
            .Append("autoLogin", "on")
            .Append("bizId", "2201001794")
            .Append("css", "https://microgame.5054399.net/v2/resource/cssSdk/default/login.css")
            .Append("displayMode", "popup")
            .Append("externalLogin", "qq")
            .Append("gameId", "wd")
            .Append("iframeId", "popup_login_frame")
            .Append("includeFcmInfo", "false")
            .Append("layout", "vertical")
            .Append("layoutSelfAdapting", "true")
            .Append("level", "8")
            .Append("loginFrom", "uframe")
            .Append("mainDivId", "popup_login_div")
            .Append("postLoginHandler", "default")
            .Append("redirectUrl", "")
            .Append("regLevel", "8")
            .Append("sec", "1")
            .Append("sessionId", "")
            .Append("userNameLabel", "4399用户名")
            .Append("userNameTip", "请输入4399用户名")
            .Append("welcomeTip", "欢迎回到4399");
    }
}
