using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.C4399;

public class C4399OpsItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;

    [JsonPropertyName("banner")] public string Banner { get; set; } = string.Empty;

    [JsonPropertyName("type")] public int Type { get; set; } = 0;
}