using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityQueryNetGameItem
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;
}
