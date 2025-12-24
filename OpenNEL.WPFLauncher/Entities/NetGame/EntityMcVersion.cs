using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityMcVersion
{
    [JsonPropertyName("mcversionid")]
    public int McVersionId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
