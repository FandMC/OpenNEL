/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Utils.Exception;
using OpenNEL_WinUI.Entities.Web.NEL;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.Utils;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class Login4399
    {
        public object Execute(string account, string password, string sessionId = null, string captcha = null)
        {
            try
            {
                AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();
                var c4399 = new Codexus.OpenSDK.C4399();
                string cookieJson = !string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(captcha)
                    ? c4399.LoginWithPasswordAsync(account, password, sessionId, captcha).GetAwaiter().GetResult()
                    : c4399.LoginWithPasswordAsync(account, password).GetAwaiter().GetResult();
                if (AppState.Debug) Log.Information("4399 Login cookieJson length: {Length}, content: {Content}", cookieJson.Length, cookieJson);
                if (string.IsNullOrWhiteSpace(cookieJson))
                {
                    var err = new { type = "login_4399_error", message = "cookie empty" };
                    return err;
                }
                EntityX19CookieRequest cookieReq;
                
                cookieReq = new EntityX19CookieRequest { Json = cookieJson };
                
                var (authOtp, channel) = AppState.X19.LoginWithCookie(cookieReq);
                if (AppState.Debug) Log.Information("X19 LoginWithCookie: {UserId} Channel: {Channel}", authOtp.EntityId, channel);
                
                if (!string.IsNullOrWhiteSpace(sessionId) && !string.IsNullOrWhiteSpace(captcha))
                {
                    var captchaUrl = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + sessionId;
                    _ = ReportCaptchaSuccessAsync(captchaUrl, captcha);
                }
                
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new OpenNEL_WinUI.Entities.Web.EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "password",
                    Details = JsonSerializer.Serialize(new EntityPasswordRequest { Account = account ?? string.Empty, Password = password ?? string.Empty })
                });
                var list = new System.Collections.ArrayList();
                list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (HttpRequestException he) when (he.Message.Contains("unactived", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("WS 4399 账号未激活. account={Account}", account ?? string.Empty);
                return new { type = "login_4399_error", message = "账号未激活，请先使用官方启动器进入游戏一次" };
            }
            catch (JsonException je)
            {
                Log.Error(je, "WS 4399 JSON解析失败. account={Account}", account ?? string.Empty);
                return new { type = "login_4399_error", message = "服务端响应异常，请稍后重试" };
            }
            catch (CaptchaException ce)
            {
                if (AppState.Debug) Log.Error(ce, "WS 4399 captcha required. account={Account}", account ?? string.Empty);
                return HandleCaptchaRequired(account, password);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var lower = msg.ToLowerInvariant();
                if (AppState.Debug) Log.Error(ex, "WS 4399 login exception. account={Account} sid={Sid}", account ?? string.Empty, sessionId ?? string.Empty);
                if (lower.Contains("parameter") && lower.Contains("'s'"))
                {
                    return HandleCaptchaRequired(account, password);
                }
                var err = new { type = "login_4399_error", message = msg.Length == 0 ? "登录失败" : msg };
                return err;
            }
        }

        private object HandleCaptchaRequired(string account, string password)
        {
            var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
            var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;

            try
            {
                var recognizedCaptcha = CaptchaRecognitionService.RecognizeFromUrlAsync(url).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(recognizedCaptcha))
                {
                    Log.Information("[Login4399] 验证码自动识别成功: {Captcha}，正在重试登录", recognizedCaptcha);
                    return Execute(account, password, captchaSid, recognizedCaptcha);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Login4399] 验证码自动识别失败: {Error}", ex.Message);
            }

            Log.Information("[Login4399] 验证码自动识别失败，需要手动输入");
            return new { type = "captcha_required", account, password, sessionId = captchaSid, captchaUrl = url };
        }

        private static async Task ReportCaptchaSuccessAsync(string captchaUrl, string captchaText)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var imageBytes = await http.GetByteArrayAsync(captchaUrl);
                var base64 = Convert.ToBase64String(imageBytes);
                await CaptchaRecognitionService.ReportSuccessAsync(base64, captchaText);
            }
            catch (Exception ex) { Log.Debug(ex, "上报验证码失败"); }
        }
    }
}
