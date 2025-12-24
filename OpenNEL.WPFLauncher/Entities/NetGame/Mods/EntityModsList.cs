using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame.Mods;

public class EntityModsList
{
    [JsonPropertyName("mods")]
    public List<EntityModsInfo> Mods { get; set; } = new();
}
