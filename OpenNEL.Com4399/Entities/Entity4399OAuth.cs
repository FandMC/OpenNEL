using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class Entity4399OAuth
{
    [JsonPropertyName("_d")]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("_d_sm")]
    public string DeviceIdentifierSm { get; set; } = string.Empty;

    [JsonPropertyName("udid")]
    public string Udid { get; set; } = string.Empty;

    [JsonPropertyName("_av")]
    public string AppVersion { get; set; } = "1.7.9";

    [JsonPropertyName("_cv")]
    public string ClientVersion { get; set; } = "6.8.0";

    [JsonPropertyName("_dt")]
    public string DeviceType { get; set; } = "Phone";

    [JsonPropertyName("_sv")]
    public string SystemVersion { get; set; } = "14";

    [JsonPropertyName("_platform")]
    public string Platform { get; set; } = "Android";
}
