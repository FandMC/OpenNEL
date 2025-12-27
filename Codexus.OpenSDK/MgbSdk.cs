using System.Text.Json;
using Codexus.OpenSDK.Entities.MgbSdk;
using Codexus.OpenSDK.Http;

namespace Codexus.OpenSDK;

public class MgbSdk(string gameId) : IDisposable
{
    private readonly HttpWrapper _sdk = new("https://mgbsdk.matrix.netease.com");

    public void Dispose()
    {
        _sdk.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task AuthenticateSessionAsync(string cookie)
    {
        if (string.IsNullOrWhiteSpace(cookie))
            throw new ArgumentException("Cookie cannot be null or empty.", nameof(cookie));

        var response = await _sdk.PostAsync($"/{gameId}/sdk/uni_sauth", cookie);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");

        var content = await response.Content.ReadAsStringAsync();

        Dictionary<string, object>? dict;
        try
        {
            dict = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException("Failed to parse SDK response JSON.", ex);
        }

        if (dict is null || !dict.TryGetValue("code", out var code))
            throw new HttpRequestException("Unexpected response format: missing 'code' field.");

        if (!string.Equals(code.ToString(), "200", StringComparison.Ordinal))
        {
            var status = dict.TryGetValue("status", out var statusVal) ? statusVal.ToString() : "Unknown";
            throw new HttpRequestException($"Authentication failed. Status: {status}");
        }
    }

    public string GenerateSAuth(
        string userId,
        string sdkUid,
        string sessionId,
        string timestamp,
        string channel,
        string platform = "pc")
    {
        var uniqueId = Guid.NewGuid().ToString("N");

        var payload = new MgbSdkSAuthJson
        {
            AppChannel = channel,
            ClientLoginSn = uniqueId,
            DeviceId = uniqueId,
            GameId = gameId,
            LoginChannel = channel,
            SdkUid = sdkUid,
            SessionId = sessionId,
            Timestamp = timestamp,
            Platform = platform,
            SourcePlatform = platform,
            Udid = uniqueId,
            UserId = userId
        };

        return JsonSerializer.Serialize(payload);
    }
}