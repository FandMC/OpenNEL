using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntitySearchByItemIdQuery
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}
