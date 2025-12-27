using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.MPay;

public class MPayDevice
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;
}