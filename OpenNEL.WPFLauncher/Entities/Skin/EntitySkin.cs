using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Skin;

public class EntitySkin
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title_image_url")]
    public string TitleImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("price_type")]
    public int PriceType { get; set; }

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("is_has")]
    public bool IsHas { get; set; }
}
