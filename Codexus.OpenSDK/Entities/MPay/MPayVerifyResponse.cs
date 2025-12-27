using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.MPay;

public class MPayVerifyResponse
{
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("code")] public int Code { get; set; } = 0;
    [JsonPropertyName("verify_url")] public string VerifyUrl { get; set; } = string.Empty;
}