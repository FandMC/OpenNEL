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
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OpenNEL_WinUI.Utils;
using Serilog;

namespace OpenNEL_WinUI.Manager;

public class AuthManager
{
    private const string TokenFilePath = "auth_token.dat";
    private const string BaseUrl = "https://api.fandmc.cn/v8";
    private static readonly HttpClient Http = new();

    public static AuthManager Instance { get; } = new();

    public string? Token { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public event Action? LoginStateChanged;

    private AuthManager()
    {
        LoadToken();
    }

    private void LoadToken()
    {
        try
        {
            if (File.Exists(TokenFilePath))
            {
                Token = File.ReadAllText(TokenFilePath).Trim();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载 Token 失败");
        }
    }

    private void SaveToken()
    {
        try
        {
            if (!string.IsNullOrEmpty(Token))
            {
                File.WriteAllText(TokenFilePath, Token);
            }
            else if (File.Exists(TokenFilePath))
            {
                File.Delete(TokenFilePath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存 Token 失败");
        }
    }

    public async Task<CaptchaResult> GetCaptchaAsync()
    {
        try
        {
            var response = await Http.GetAsync($"{BaseUrl}/captcha");
            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("[AuthManager] Captcha response: {Content}", content);
            if (!response.IsSuccessStatusCode)
            {
                return new CaptchaResult { Success = false, Message = "获取验证码失败" };
            }
            var result = JsonSerializer.Deserialize<CaptchaResponse>(content);
            return new CaptchaResult
            {
                Success = true,
                CaptchaId = result?.CaptchaId ?? string.Empty,
                ImageBase64 = result?.ImageBase64 ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取验证码异常");
            return new CaptchaResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResult> VerifyAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Token))
            {
                return new AuthResult { Success = false, Message = "Token 不存在" };
            }
            var hwid = Hwid.Compute();
            var payload = JsonSerializer.Serialize(new { token = Token, hwid });
            var response = await Http.PostAsync($"{BaseUrl}/verify",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            Log.Debug("[AuthManager] Verify status: {Status}", response.StatusCode);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                LoginStateChanged?.Invoke();
                return new AuthResult { Success = true, Token = Token };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new AuthResult { Success = false, Message = "机器码校验失败" };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new AuthResult { Success = false, Message = "Token 无效" };
            }
            else
            {
                return new AuthResult { Success = false, Message = "验证失败" };
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "验证异常");
            return new AuthResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var hwid = Hwid.Compute();
            var payload = JsonSerializer.Serialize(new { username, password, hwid });
            var response = await Http.PostAsync($"{BaseUrl}/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("[AuthManager] Login response: {Status} {Content}", response.StatusCode, content);

            if (response.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(content))
            {
                var result = JsonSerializer.Deserialize<TokenResponse>(content);
                if (!string.IsNullOrEmpty(result?.Token))
                {
                    Token = result.Token;
                    SaveToken();
                    LoginStateChanged?.Invoke();
                    return new AuthResult { Success = true, Token = Token, Username = result.Username };
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new AuthResult { Success = false, Message = "用户名或密码错误" };
            }

            return new AuthResult { Success = false, Message = $"登录失败 ({response.StatusCode})" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "登录异常");
            return new AuthResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string password, string captchaId, string captchaText)
    {
        try
        {
            var hwid = Hwid.Compute();
            var payload = JsonSerializer.Serialize(new
            {
                username,
                password,
                captchaId,
                captchaText,
                hwid
            });
            var response = await Http.PostAsync($"{BaseUrl}/register",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("[AuthManager] Register request: {Payload}", payload);
            Log.Debug("[AuthManager] Register response: {Status} {Content}", response.StatusCode, content);

            if (response.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(content))
            {
                var result = JsonSerializer.Deserialize<TokenResponse>(content);
                if (!string.IsNullOrEmpty(result?.Token))
                {
                    Token = result.Token;
                    SaveToken();
                    LoginStateChanged?.Invoke();
                    return new AuthResult { Success = true, Token = Token, Username = result.Username };
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(content);
                    if (!string.IsNullOrEmpty(error?.Message))
                    {
                        return new AuthResult { Success = false, Message = error.Message };
                    }
                }
                catch { }
            }

            return new AuthResult { Success = false, Message = $"注册失败 ({response.StatusCode})" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "注册异常");
            return new AuthResult { Success = false, Message = ex.Message };
        }
    }

    public void Logout()
    {
        Token = null;
        SaveToken();
        LoginStateChanged?.Invoke();
    }

    public async Task<UserInfoResult> GetUserAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Token))
            {
                return new UserInfoResult { Success = false, Message = "未登录" };
            }
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/getuser");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            var response = await Http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("[AuthManager] GetUser response: {Status} {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
            {
                var result = JsonSerializer.Deserialize<UserInfoResponse>(content);
                if (result != null)
                {
                    return new UserInfoResult
                    {
                        Success = true,
                        Id = result.Id,
                        Username = result.Username,
                        CreatedAt = result.CreatedAt,
                        LastLogin = result.LastLogin,
                        Rank = result.Rank
                    };
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new UserInfoResult { Success = false, Message = "Token 无效" };
            }

            return new UserInfoResult { Success = false, Message = "获取用户信息失败" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取用户信息异常");
            return new UserInfoResult { Success = false, Message = ex.Message };
        }
    }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Message { get; set; }
}

public class CaptchaResult
{
    public bool Success { get; set; }
    public string CaptchaId { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class CaptchaResponse
{
    [JsonPropertyName("id")]
    public string CaptchaId { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string ImageBase64 { get; set; } = string.Empty;
}

public class TokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class UserInfoResult
{
    public bool Success { get; set; }
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? Rank { get; set; }
    public string? Message { get; set; }
}

public class UserInfoResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("lastLogin")]
    public DateTime? LastLogin { get; set; }

    [JsonPropertyName("rank")]
    public string? Rank { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
