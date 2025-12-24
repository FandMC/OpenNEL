using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityPatchVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
