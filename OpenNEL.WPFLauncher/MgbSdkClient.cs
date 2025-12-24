using System.Text.Encodings.Web;
using System.Text.Json;
using OpenNEL.Core.Http;
using OpenNEL.WPFLauncher.Entities;

namespace OpenNEL.WPFLauncher;

public class MgbSdkClient : IDisposable
{
    private readonly HttpWrapper _sdk = new("https://mgbsdk.matrix.netease.com");
    private readonly string _gameId;

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public MgbSdkClient(string gameId)
    {
        _gameId = gameId;
    }

    public void Dispose()
    {
        _sdk.Dispose();
        GC.SuppressFinalize(this);
    }

    public string GenerateSAuth(string deviceId, string userid, string sdkUid, string sessionId, string timestamp, string channel, string platform = "pc")
    {
        var sessionIdUpper = sessionId.ToUpper();
        return JsonSerializer.Serialize(new EntityMgbSdkCookie
        {
            Ip = "127.0.0.1",
            AimInfo = "{\"aim\":\"127.0.0.1\",\"tz\":\"+0800\",\"tzid\":\"\",\"country\":\"CN\"}",
            AppChannel = channel,
            ClientLoginSn = deviceId.ToUpper(),
            DeviceId = deviceId.ToUpper(),
            GameId = _gameId,
            LoginChannel = channel,
            SdkUid = sdkUid,
            SessionId = sessionIdUpper,
            Timestamp = timestamp,
            Platform = platform,
            SourcePlatform = platform,
            Udid = deviceId.ToUpper(),
            UserId = userid
        }, DefaultOptions);
    }

    public async Task AuthSessionAsync(string cookie)
    {
        var response = await _sdk.PostAsync("/" + _gameId + "/sdk/uni_sauth", cookie);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(response.ReasonPhrase);
        }
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
        if (dict?["code"].ToString() != "200")
        {
            throw new HttpRequestException("Status: " + dict?["status"]);
        }
    }
}
