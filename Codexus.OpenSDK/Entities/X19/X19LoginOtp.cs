using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19LoginOtp
{
    [JsonPropertyName("otp")] private int Otp { get; set; } = 0;
    [JsonPropertyName("otp_token")] public string OtpToken { get; set; } = string.Empty;
    [JsonPropertyName("aid")] public int Aid { get; set; } = 0;
    [JsonPropertyName("lock_time")] public int LockTime { get; set; } = 0;
    [JsonPropertyName("open_otp")] public int OpenOtp { get; set; } = 0;
}