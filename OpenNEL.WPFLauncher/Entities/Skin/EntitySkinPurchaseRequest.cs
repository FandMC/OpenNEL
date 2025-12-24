using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Skin;

public class EntitySkinPurchaseRequest
{
    [JsonPropertyName("batch_count")]
    public int BatchCount { get; set; }

    [JsonPropertyName("buy_path")]
    public string BuyPath { get; set; } = string.Empty;

    [JsonPropertyName("diamond")]
    public int Diamond { get; set; }

    [JsonPropertyName("entity_id")]
    public int EntityId { get; set; }

    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("item_level")]
    public int ItemLevel { get; set; }

    [JsonPropertyName("last_play_time")]
    public int LastPlayTime { get; set; }

    [JsonPropertyName("purchase_time")]
    public int PurchaseTime { get; set; }

    [JsonPropertyName("total_play_time")]
    public int TotalPlayTime { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}
