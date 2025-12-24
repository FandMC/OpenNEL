using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Skin;

public class EntityQuerySkinByNameRequest
{
    [JsonPropertyName("is_has")]
    public bool IsHas { get; set; }

    [JsonPropertyName("is_sync")]
    public int IsSync { get; set; }

    [JsonPropertyName("item_type")]
    public int ItemType { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("master_type_id")]
    public int MasterTypeId { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("price_type")]
    public int PriceType { get; set; }

    [JsonPropertyName("secondary_type_id")]
    public string SecondaryTypeId { get; set; } = string.Empty;

    [JsonPropertyName("sort_type")]
    public int SortType { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}
