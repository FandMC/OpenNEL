using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityQueryNetGameRequest
{
    [JsonPropertyName("entity_ids")]
    public string[] EntityIds { get; set; } = Array.Empty<string>();
}
