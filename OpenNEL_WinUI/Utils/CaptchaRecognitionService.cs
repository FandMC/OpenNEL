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
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private class CaptchaResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }
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
        Func<string, Task<string>> manualInputAsync)
    {
        var result = await RecognizeFromUrlAsync(captchaUrl);
        if (!string.IsNullOrWhiteSpace(result))
        {
            Log.Information("[CaptchaRecognition] 使用自动识别的验证码: {Result}", result);
            return result;
        }
        Log.Information("[CaptchaRecognition] 自动识别失败，需要手动输入验证码");
        return await manualInputAsync(captchaUrl);
    }
}
