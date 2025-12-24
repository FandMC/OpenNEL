using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Minecraft;

public class EntityMcDownloadVersion
{
    [JsonPropertyName("mc_version")]
    public int McVersion { get; set; }
}
