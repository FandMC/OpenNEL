/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;

namespace OpenNEL_WinUI.Utils;

public static class CaptchaRecognitionService
{
    private const string ApiUrl = "https://api.fandmc.cn/v9/captcha";
    private const string ReportUrl = "https://api.fandmc.cn/v7/cc";
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private class CaptchaResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }

    public static async Task ReportSuccessAsync(string base64, string captchaText)
    {
        try
        {
            var requestBody = JsonSerializer.Serialize(new { base64, text = captchaText });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(ReportUrl, content);
            Log.Debug("[CaptchaRecognition] 验证码成功数据已上报");
        }
        catch (Exception ex)
        {
            Log.Debug("[CaptchaRecognition] 上报失败: {Error}", ex.Message);
        }
    }

    public static async Task<string?> RecognizeFromUrlAsync(string captchaUrl)
    {
        try
        {
            Log.Debug("[CaptchaRecognition] 正在下载验证码图片: {Url}", captchaUrl);
            var imageBytes = await _httpClient.GetByteArrayAsync(captchaUrl);
            var base64 = Convert.ToBase64String(imageBytes);
            return await RecognizeFromBase64Async(base64);
        }
        catch (Exception ex)
        {
            Log.Warning("[CaptchaRecognition] 从URL识别验证码失败: {Error}", ex.Message);
            return null;
        }
    }

    public static async Task<string?> RecognizeFromBase64Async(string base64)
    {
        try
        {
            Log.Debug("[CaptchaRecognition] 正在调用验证码识别 API");
            var requestBody = JsonSerializer.Serialize(new { base64 });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ApiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("[CaptchaRecognition] API 返回错误状态码: {StatusCode}", response.StatusCode);
                return null;
            }
            var responseJson = await response.Content.ReadAsStringAsync();
            Log.Debug("[CaptchaRecognition] API 响应: {Response}", responseJson);
            var result = JsonSerializer.Deserialize<CaptchaResponse>(responseJson);
            if (string.IsNullOrWhiteSpace(result?.Result))
            {
                Log.Warning("[CaptchaRecognition] API 返回空结果");
                return null;
            }
            Log.Information("[CaptchaRecognition] 验证码识别成功: {Result}", result.Result);
            _ = ReportSuccessAsync(base64, result.Result);
            return result.Result;
        }
        catch (TaskCanceledException)
        {
            Log.Warning("[CaptchaRecognition] 验证码识别请求超时");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Log.Warning("[CaptchaRecognition] 无法连接到验证码识别服务器: {Error}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Log.Warning("[CaptchaRecognition] 验证码识别失败: {Error}", ex.Message);
            return null;
        }
    }

    public static async Task<string> RecognizeOrManualInputAsync(
        string captchaUrl,
        Func<string, Task<string>> manualInputAsync,
        int maxRetries = 3)
    {
        var (result, _) = await RecognizeWithRetryAsync(captchaUrl, manualInputAsync, maxRetries);
        return result;
    }

    public static async Task<(string captcha, string usedUrl)> RecognizeWithRetryAsync(
        string captchaUrl,
        Func<string, Task<string>>? manualInputAsync = null,
        int maxRetries = 3)
    {
        string currentUrl = captchaUrl;
        for (int i = 0; i < maxRetries; i++)
        {
            if (i > 0)
            {
                currentUrl = RegenerateCaptchaUrl(captchaUrl);
            }
            var result = await RecognizeFromUrlAsync(currentUrl);
            if (!string.IsNullOrWhiteSpace(result))
            {
                Log.Information("[CaptchaRecognition] 使用自动识别的验证码: {Result} (第{Attempt}次尝试)", result, i + 1);
                return (result, currentUrl);
            }
            Log.Information("[CaptchaRecognition] 第{Attempt}次自动识别失败", i + 1);
            if (i < maxRetries - 1)
            {
                await Task.Delay(200);
            }
        }
        Log.Information("[CaptchaRecognition] 自动识别{MaxRetries}次均失败", maxRetries);
        if (manualInputAsync != null)
        {
            var manualResult = await manualInputAsync(currentUrl);
            return (manualResult, currentUrl);
        }
        return (string.Empty, currentUrl);
    }

    private static string RegenerateCaptchaUrl(string originalUrl)
    {
        var uri = new Uri(originalUrl);
        var newCaptchaId = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?captchaId={newCaptchaId}";
    }
}
