/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
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
                Log.Information("[AuthManager] Token 已加载: {Token}", Token);
            }
            else
            {
                Log.Debug("[AuthManager] 未找到 Token 文件，需要登录或注册");
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
            var fullPath = Path.GetFullPath(TokenFilePath);
            if (!string.IsNullOrEmpty(Token))
            {
                File.WriteAllText(TokenFilePath, Token);
                Log.Information("[AuthManager] Token 已保存到: {Path}, Token: {Token}", fullPath, Token);
            }
            else if (File.Exists(TokenFilePath))
            {
                File.Delete(TokenFilePath);
                Log.Information("[AuthManager] Token 文件已删除: {Path}", fullPath);
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
            Log.Information("[AuthManager] Register request: {Payload}", payload);
            Log.Information("[AuthManager] Register response: {Status} {Content}", response.StatusCode, content);

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
                    if (!string.IsNullOrEmpty(error?.Error))
                    {
                        return new AuthResult { Success = false, Message = error.Error };
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
            var hwid = Hwid.Compute();
            var payload = JsonSerializer.Serialize(new { token = Token, hwid });
            Log.Information("[AuthManager] GetUser request: {Payload}", payload);
            var response = await Http.PostAsync($"{BaseUrl}/getuser",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            Log.Information("[AuthManager] GetUser response: {Status} {Content}", response.StatusCode, content);

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
                        Rank = result.Rank,
                        Avatar = result.Avatar,
                        Banned = result.Banned,
                        IsAdmin = result.IsAdmin,
                        Hwid = result.Hwid
                    };
                }
            }

            // 解析错误响应
            var errMsg = "获取用户信息失败";
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("error", out var errEl))
                    errMsg = errEl.GetString() ?? errMsg;
            }
            catch { }

            return new UserInfoResult { Success = false, Message = errMsg };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取用户信息异常");
            return new UserInfoResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<WebKeyResult> GenerateWebKeyAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Token))
            {
                return new WebKeyResult { Success = false, Message = "未登录" };
            }
            var hwid = Hwid.Compute();
            var payload = JsonSerializer.Serialize(new { token = Token, hwid });
            var response = await Http.PostAsync($"{BaseUrl}/webkey",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("[AuthManager] WebKey response: {Status} {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
            {
                var result = JsonSerializer.Deserialize<WebKeyResponse>(content);
                if (!string.IsNullOrEmpty(result?.Key))
                {
                    return new WebKeyResult { Success = true, Key = result.Key };
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new WebKeyResult { Success = false, Message = "认证失败" };
            }

            return new WebKeyResult { Success = false, Message = "生成密钥失败" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "生成网页密钥异常");
            return new WebKeyResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<AvatarResult> UpdateAvatarAsync(string? avatarBase64)
    {
        try
        {
            if (string.IsNullOrEmpty(Token))
            {
                return new AvatarResult { Success = false, Message = "未登录" };
            }
            var hwid = Hwid.Compute();
            var payload = JsonSerializer.Serialize(new { token = Token, hwid, avatar = avatarBase64 });
            var response = await Http.PostAsync($"{BaseUrl}/avatar",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();
            Log.Debug("[AuthManager] Avatar response: {Status} {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("success", out var successEl) && successEl.GetBoolean())
                {
                    return new AvatarResult { Success = true };
                }
            }

            using var errDoc = JsonDocument.Parse(content);
            var errMsg = errDoc.RootElement.TryGetProperty("error", out var errEl) ? errEl.GetString() : "更新头像失败";
            return new AvatarResult { Success = false, Message = errMsg };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新头像异常");
            return new AvatarResult { Success = false, Message = ex.Message };
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
    public string? Avatar { get; set; }
    public bool Banned { get; set; }
    public bool IsAdmin { get; set; }
    public string? Hwid { get; set; }
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

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("banned")]
    public bool Banned { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("hwid")]
    public string? Hwid { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}

public class WebKeyResult
{
    public bool Success { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class WebKeyResponse
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}

public class AvatarResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
