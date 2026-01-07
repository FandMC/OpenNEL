/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections;
using System.Text.Json;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Exception;
using OpenNEL_WinUI.Entities.Web;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Login;

public class LoginX19
{
    public object Execute(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new { type = "login_x19_error", message = "邮箱或密码不能为空" };
            }

            AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();

            var wpf = AppState.X19;
            Log.Information("[LoginX19] 正在使用邮箱登录: {Email}", email);
            var mPayUser = wpf.LoginWithEmailAsync(email, password).GetAwaiter().GetResult();
            Log.Information("[LoginX19] 邮箱登录成功，获取设备信息");
            var device = wpf.MPay.GetDevice();
            var cookieRequest = WPFLauncher.GenerateCookie(mPayUser, device);
            Log.Information("[LoginX19] 生成Cookie成功，正在登录");
            var (authOtp, channel) = wpf.LoginWithCookie(cookieRequest);

            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = authOtp.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = channel,
                Type = "netease",
                Details = JsonSerializer.Serialize(new { email, password })
            });

            var list = new ArrayList();
            list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            var items = GetAccount.GetAccountItems();
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (VerifyException ve)
        {
            Log.Error(ve, "[LoginX19] 验证失败");
            return new { type = "login_x19_error", message = ve.Message};
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LoginX19] 登录异常");
            var msg = ex.Message;
            if (msg.Contains("password") || msg.Contains("密码"))
            {
                return new { type = "login_x19_error", message = "邮箱或密码错误" };
            }
            return new { type = "login_x19_error", message = msg.Length == 0 ? "登录失败" : msg };
        }
    }
}
