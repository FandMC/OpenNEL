using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityAuthenticationVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
