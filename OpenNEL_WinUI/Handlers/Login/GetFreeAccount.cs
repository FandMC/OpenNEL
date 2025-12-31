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
using System.Text.Json;
using System.Threading.Tasks;
using OpenNEL_WinUI.type;
using Serilog;
using OpenNEL_WinUI.Utils;
using OpenNEL_WinUI.Utils;
using Codexus.Development.SDK.Entities;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class GetFreeAccount
    {
        public async Task<object[]> Execute(string hwid = null,
                                            int timeoutSec = 30,
                                            string userAgent = null,
                                            int maxRetries = 3,
                                            Func<string, Task<string>> inputCaptchaAsync = null)
        {
            Log.Information("正在获取4399小号...");
            var status = new { type = "get_free_account_status", status = "processing", message = "获取小号中, 这可能需要点时间..." };
            object? resultPayload = null;
            try
            {
                var hw = string.IsNullOrWhiteSpace(hwid) ? Hwid.Compute() : hwid;
                if (string.IsNullOrWhiteSpace(hw))
                {
                    resultPayload = new { type = "get_free_account_result", success = false, message = "空请求体" };
                    return new object[] { status, resultPayload };
                }
                if (!IsValidHwid(hw))
                {
                    resultPayload = new { type = "get_free_account_result", success = false, message = "请求错误" };
                    return new object[] { status, resultPayload };
                }
                if (inputCaptchaAsync == null)
                {
                    resultPayload = new { type = "get_free_account_result", success = false, message = "缺少验证码输入" };
                    return new object[] { status, resultPayload };
                }
                using var reg = new Channel4399Register();
                var acc = await reg.RegisterAsync(inputCaptchaAsync, () => new IdCard
                {
                    IdNumber = Channel4399Register.GenerateRandomIdCard(),
                    Name = Channel4399Register.GenerateChineseName()
                });
                Log.Information("获取成功: {Account} {Password}", acc.Account, acc.Password);
                resultPayload = new
                {
                    type = "get_free_account_result",
                    success = true,
                    username = acc.Account,
                    password = acc.Password,
                    cookie = (string)null,
                    message = "获取成功！"
                };
            }
            catch (Exception e)
            {
                Log.Error(e, "错误: {Message}", e.Message);
                resultPayload = new { type = "get_free_account_result", success = false, message = "错误: " + e.Message };
            }
            return new object[] { status, resultPayload ?? new { type = "get_free_account_result", success = false, message = "未知错误" } };
        }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            if (v.ValueKind == JsonValueKind.Number) return v.ToString();
            if (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) return v.ToString();
        }
        return null;
    }

    private static bool IsValidHwid(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Length > 256) return false;
        foreach (var ch in s)
        {
            var ok = (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
            if (!ok) return false;
        }
        return true;
    }
    }
}
