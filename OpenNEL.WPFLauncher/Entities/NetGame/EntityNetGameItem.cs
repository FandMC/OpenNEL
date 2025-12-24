using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityNetGameItem
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title_image_url")]
    public string TitleImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("item_type")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("online_count")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string OnlineCount { get; set; } = string.Empty;

    [JsonPropertyName("total_play_count")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string TotalPlayCount { get; set; } = string.Empty;

    [JsonPropertyName("star")]
    public double Star { get; set; }
}
