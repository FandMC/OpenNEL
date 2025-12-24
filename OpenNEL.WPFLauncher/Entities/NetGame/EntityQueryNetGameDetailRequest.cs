using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityQueryNetGameDetailRequest
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;
}
