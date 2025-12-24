using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Skin;

public class EntitySkinDetailsRequest
{
    [JsonPropertyName("channel_id")]
    public int ChannelId { get; set; }

    [JsonPropertyName("entity_ids")]
    public List<string> EntityIds { get; set; } = new();

    [JsonPropertyName("is_has")]
    public bool IsHas { get; set; }

    [JsonPropertyName("with_price")]
    public bool WithPrice { get; set; }

    [JsonPropertyName("with_title_image")]
    public bool WithTitleImage { get; set; }
}
