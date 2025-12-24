using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Skin;

public class EntityFreeSkinListRequest
{
    [JsonPropertyName("is_has")]
    public bool IsHas { get; set; }

    [JsonPropertyName("item_type")]
    public int ItemType { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("master_type_id")]
    public int MasterTypeId { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("price_type")]
    public int PriceType { get; set; }

    [JsonPropertyName("secondary_type_id")]
    public int SecondaryTypeId { get; set; }
}
