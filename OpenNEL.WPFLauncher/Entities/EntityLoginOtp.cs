using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityLoginOtp
{
    [JsonPropertyName("aid")]
    public long Aid { get; set; }

    [JsonPropertyName("otp_token")]
    public string OtpToken { get; set; } = string.Empty;
}
