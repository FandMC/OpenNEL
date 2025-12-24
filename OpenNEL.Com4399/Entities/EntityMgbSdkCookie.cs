using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class EntityMgbSdkCookie
{
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("aim_info")]
    public string AimInfo { get; set; } = string.Empty;

    [JsonPropertyName("app_channel")]
    public string AppChannel { get; set; } = string.Empty;

    [JsonPropertyName("client_login_sn")]
    public string ClientLoginSn { get; set; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("gameid")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("login_channel")]
    public string LoginChannel { get; set; } = string.Empty;

    [JsonPropertyName("sdkuid")]
    public string SdkUid { get; set; } = string.Empty;

    [JsonPropertyName("sessionid")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("source_platform")]
    public string SourcePlatform { get; set; } = string.Empty;

    [JsonPropertyName("udid")]
    public string Udid { get; set; } = string.Empty;

    [JsonPropertyName("userid")]
    public string UserId { get; set; } = string.Empty;
}
