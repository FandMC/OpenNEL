using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityNetGameKeyword
{
    [JsonPropertyName("is_has")]
    public bool IsHas { get; set; }

    [JsonPropertyName("is_sync")]
    public int IsSync { get; set; }

    [JsonPropertyName("item_type")]
    public int ItemType { get; set; } = 1;

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("length")]
    public int Length { get; set; } = 24;

    [JsonPropertyName("master_type_id")]
    public string MasterTypeId { get; set; } = "2";

    [JsonPropertyName("network_tag")]
    public bool NetworkTag { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("secondary_type_id")]
    public string SecondaryTypeId { get; set; } = "";

    [JsonPropertyName("sort_type")]
    public int SortType { get; set; } = 2;

    [JsonPropertyName("year")]
    public int Year { get; set; }
}
