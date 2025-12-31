/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System.Net;
using System.Text.Json;
using OpenNEL.Core.Http;
using OpenNEL.Core.Utils;
using OpenNEL.MPay;
using OpenNEL.MPay.Exceptions;
using OpenNEL.Pc4399.Entities;
using OpenNEL.WPFLauncher;

namespace OpenNEL.Pc4399;

public sealed class Pc4399Client : IDisposable
{
    private const string AppId = "kid_wdsj";
    private const string GameUrl = "https://cdn.h5wan.4399sj.com/microterminal-h5-frame?game_id=500352";
    private const string LoginEndpoint = "https://ptlogin.4399.com";
    private const string ServiceEndpoint = "https://microgame.5054399.net";

    private readonly CookieContainer _cookies;
    private readonly HttpWrapper _loginClient;
    private readonly HttpWrapper _serviceClient;
    private readonly MgbSdkClient _sdkClient;
    private bool _disposed;

    public MPayClient MPay { get; }

    public Pc4399Client()
    {
        var gameVersion = WPFLauncherClient.GetLatestVersionAsync().GetAwaiter().GetResult();
        MPay = new MPayClient("aecfrxodyqaaaajp-g-x19", gameVersion);
        
        _cookies = new CookieContainer();
        _loginClient = new HttpWrapper(LoginEndpoint, null, new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true,
            AllowAutoRedirect = true
        });
        _serviceClient = new HttpWrapper(ServiceEndpoint);
        _sdkClient = new MgbSdkClient("x19");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        MPay.Dispose();
        _loginClient.Dispose();
        _serviceClient.Dispose();
        _sdkClient.Dispose();
        _cookies.GetAllCookies().Clear();
        GC.SuppressFinalize(this);
    }

    public async Task<string> LoginWithPasswordAsync(string username, string password, string? captchaIdentifier = null, string? captcha = null)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var loginParams = BuildLoginParameters()
            .Append("username", username)
            .Append("password", password);

        var hasCaptcha = !string.IsNullOrWhiteSpace(captchaIdentifier) && !string.IsNullOrWhiteSpace(captcha);
        
        if (!hasCaptcha)
        {
            var checkResponse = await _loginClient.PostAsync(
                "/ptlogin/loginFrame.do?v=1",
                loginParams.FormUrlEncode(),
                "application/x-www-form-urlencoded");
            
            var checkContent = await checkResponse.Content.ReadAsStringAsync();
            if (checkContent.Contains("账号异常，请输入验证码"))
                throw new CaptchaException("Captcha required for login");
        }

        if (hasCaptcha)
        {
            loginParams.Append("sessionId", captchaIdentifier!).Append("inputCaptcha", captcha!);
        }

        var loginResponse = await _loginClient.PostAsync(
            "/ptlogin/login.do?v=1",
            loginParams.FormUrlEncode(),
            "application/x-www-form-urlencoded");

        if (!loginResponse.IsSuccessStatusCode)
            throw new Exception("Login to 4399 PC failed");

        return await GenerateCookieAsync();
    }

    private async Task<string> GenerateCookieAsync()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var redirectUrl = BuildRedirectUrl(timestamp);
        
        var checkParams = new ParameterBuilder()
            .Append("appId", AppId)
            .Append("gameUrl", GameUrl)
            .Append("isCrossDomain", "1")
            .Append("nick", "null")
            .Append("onLineStart", "false")
            .Append("ptLogin", "true")
            .Append("rand_time", "$randTime")
            .Append("retUrl", redirectUrl)
            .Append("show", "1")
            .FormUrlEncode();

        var response = await _loginClient.GetAsync($"/ptlogin/checkKidLoginUserCookie.do?{checkParams}");
        
        if (response.RequestMessage?.RequestUri == null)
            throw new Exception("Failed to get login cookie");

        var finalUri = response.RequestMessage.RequestUri.ToString();
        var queryStart = finalUri.LastIndexOf('?') + 1;
        var queryParams = finalUri[queryStart..];

        var authInfo = await FetchUniAuthAsync(queryParams);
        
        return _sdkClient.GenerateSAuth(
            MPay.Unique,
            authInfo.Get("username"),
            authInfo.Get("uid"),
            authInfo.Get("token"),
            authInfo.Get("time"),
            "4399pc");
    }

    private async Task<ParameterBuilder> FetchUniAuthAsync(string queryParameter)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var callbackName = $"jQuery1830{StringGenerator.GenerateRandomString(16, true, false, false)}_{timestamp}";
        
        var requestParams = new ParameterBuilder()
            .Append("callback", callbackName)
            .Append("queryStr", queryParameter)
            .FormUrlEncode();

        var response = await _serviceClient.GetAsync($"/v2/service/sdk/info?{requestParams}");
        
        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to fetch uni-auth data");

        var content = await response.Content.ReadAsStringAsync();
        var startIndex = content.IndexOf($"{callbackName}(", StringComparison.Ordinal) + callbackName.Length + 1;
        var jsonContent = content[startIndex..^1];
        
        var parsedResponse = JsonSerializer.Deserialize<Entity4399Response>(jsonContent)
            ?? throw new Exception("Failed to parse uni-auth response");
        
        return new ParameterBuilder(parsedResponse.Data.SdkLoginData);
    }

    private static ParameterBuilder BuildLoginParameters()
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

    private static string BuildRedirectUrl(long timestamp)
    {
        return $"https://ptlogin.4399.com/resource/ucenter.html?action=login&appId=kid_wdsj&loginLevel=8" +
               $"&regLevel=8&bizId=2201001794&externalLogin=qq&qrLogin=true&layout=vertical&level=101" +
               $"&css=https://microgame.5054399.net/v2/resource/cssSdk/default/login.css&v=2018_11_26_16" +
               $"&postLoginHandler=redirect&checkLoginUserCookie=true" +
               $"&redirectUrl=http%3A%2F%2Fcdn.h5wan.4399sj.com%2Fmicroterminal-h5-frame%3Fgame_id%3D500352%26rand_time%3D{timestamp}";
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
