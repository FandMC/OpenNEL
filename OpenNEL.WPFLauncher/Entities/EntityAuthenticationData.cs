using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityAuthenticationData
{
    [JsonPropertyName("sa_data")]
    public string SaData { get; set; } = string.Empty;

    [JsonPropertyName("sauth_json")]
    public string AuthJson { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public EntityAuthenticationVersion Version { get; set; } = new();

    [JsonPropertyName("aid")]
    public string Aid { get; set; } = string.Empty;

    [JsonPropertyName("otp_token")]
    public string OtpToken { get; set; } = string.Empty;

    [JsonPropertyName("lock_time")]
    public int LockTime { get; set; }
}
