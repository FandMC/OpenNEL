using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenNEL.Core.Utils;

public static class CrcSalt
{
    private const string Default = "E520638AC4C3C93A1188664010769EEC";
    private const string CrcSaltEndpoint = "https://api.fandmc.cn/v1/crcsalt";
    
    private static string Cached = Default;
    private static DateTime LastFetch = DateTime.MinValue;
    private static readonly TimeSpan Refresh = TimeSpan.FromHours(1);

    public static async Task<string> Compute()
    {
        if (DateTime.UtcNow - LastFetch < Refresh) return Cached;
        try
        {
            var hwid = Hwid.Compute();
            using var client = new HttpClient();
            using var content = new StringContent(hwid, Encoding.UTF8, "text/plain");
            var resp = await client.PostAsync(CrcSaltEndpoint, content);
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Cached = Default;
                LastFetch = DateTime.UtcNow;
                return Cached;
            }
            var obj = JsonSerializer.Deserialize<CrcSaltResponse>(json);
            if (obj == null || obj.success != true || string.IsNullOrWhiteSpace(obj.crcSalt))
            {
                Cached = Default;
                LastFetch = DateTime.UtcNow;
                return Cached;
            }
            Cached = obj.crcSalt;
            LastFetch = DateTime.UtcNow;
            return Cached;
        }
        catch
        {
            Cached = Default;
            LastFetch = DateTime.UtcNow;
            return Cached;
        }
    }

    public static string GetCached() => Cached;

    private record CrcSaltResponse(bool success, string? crcSalt, string? gameVersion, string? error);
}
