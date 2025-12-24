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
using Serilog;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenNEL_WinUI.type;

namespace OpenNEL_WinUI.Utils;

public static class CrcSalt
{
    static readonly string Default = "E520638AC4C3C93A1188664010769EEC";
    static string Cached = Default;
    static DateTime LastFetch = DateTime.MinValue;
    static readonly TimeSpan Refresh = TimeSpan.FromHours(1);

    public static async Task<string> Compute()
    {
        if (DateTime.UtcNow - LastFetch < Refresh) return Cached;
        try
        {
            var hwid = Hwid.Compute();
            using var client = new HttpClient();
            using var content = new StringContent(hwid, Encoding.UTF8, "text/plain");
            var resp = await client.PostAsync(AppInfo.CrcSaltEndpoint, content);
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Log.Error("CRC盐请求失败: {Code} {Body}", (int)resp.StatusCode, json);
                Cached = Default;
                LastFetch = DateTime.UtcNow;
                return Cached;
            }
            var obj = JsonSerializer.Deserialize<CrcSaltResponse>(json);
            if (obj == null || obj.success != true || string.IsNullOrWhiteSpace(obj.crcSalt))
            {
                Log.Error("CRC盐响应无效: {Body}", json);
                Cached = Default;
                LastFetch = DateTime.UtcNow;
                return Cached;
            }
            Cached = obj.crcSalt;
            LastFetch = DateTime.UtcNow;
            Log.Information("CRC请求成功: {Body}", json);
            return Cached;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CRC盐请求异常");
            Cached = Default;
            LastFetch = DateTime.UtcNow;
            return Cached;
        }
    }

    record CrcSaltResponse(bool success, string? crcSalt, string? gameVersion, string? error);
}
