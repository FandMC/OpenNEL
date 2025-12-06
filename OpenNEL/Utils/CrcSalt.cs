using Serilog;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using OpenNEL.type;

namespace OpenNEL.Utils;

public static class CrcSalt
{
    static readonly string Default = "6682E0F553668A406E16A99B6D76E283";
    static string Cached = Default;
    static DateTime LastFetch = DateTime.MinValue;
    static readonly TimeSpan Refresh = TimeSpan.FromHours(1);

    public static async Task<string> Compute()
    {
        if ((DateTime.UtcNow - LastFetch) < Refresh) return Cached;
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
