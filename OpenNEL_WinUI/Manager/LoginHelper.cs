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
using System;
using System.Threading.Tasks;
using OpenNEL.Core.Utils;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Manager;

public static class LoginHelper
{
    public static async Task<bool> TryAutoLoginAsync()
    {
        if (!AuthManager.Instance.IsLoggedIn) return false;
        try
        {
            var result = await Task.Run(async () => await AuthManager.Instance.VerifyAsync());
            return result.Success;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "自动登录失败");
            return false;
        }
    }

    public static async Task<LoginResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return new LoginResult { Success = false, Error = "用户名和密码不能为空" };

        var result = await Task.Run(async () => await AuthManager.Instance.LoginAsync(username, password));
        if (result.Success)
        {
            var welcomeName = string.IsNullOrEmpty(result.Username) ? username : result.Username;
            var prepared = await PrepareAfterLoginAsync();
            return new LoginResult { Success = true, WelcomeName = welcomeName, Prepared = prepared };
        }
        return new LoginResult { Success = false, Error = result.Message ?? "登录失败" };
    }

    public static async Task<LoginResult> RegisterAsync(string username, string password, string captchaId, string captchaText)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return new LoginResult { Success = false, Error = "用户名和密码不能为空" };
        if (string.IsNullOrEmpty(captchaText))
            return new LoginResult { Success = false, Error = "请输入验证码" };

        var result = await Task.Run(async () => await AuthManager.Instance.RegisterAsync(username, password, captchaId, captchaText));
        if (result.Success)
        {
            var welcomeName = string.IsNullOrEmpty(result.Username) ? username : result.Username;
            var prepared = await PrepareAfterLoginAsync();
            return new LoginResult { Success = true, WelcomeName = welcomeName, Prepared = prepared };
        }
        return new LoginResult { Success = false, Error = result.Message ?? "注册失败" };
    }

    public static async Task<bool> PrepareAfterLoginAsync()
    {
        try
        {
            return await Task.Run(async () =>
            {
                CrcSalt.TokenProvider = () => AuthManager.Instance.Token ?? "";
                CrcSalt.InvalidateCache();
                await CrcSalt.Compute();
                AppState.Services?.RefreshYggdrasil();
                return true;
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "登录后初始化失败");
            return false;
        }
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? WelcomeName { get; set; }
    public string? Error { get; set; }
    public bool Prepared { get; set; }
}
