using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.NetGame;

public class EntityNetGame
{
    [JsonPropertyName("res")]
    public List<EntityNetGameItem> Res { get; set; } = new();

    [JsonPropertyName("tag")]
    public int Tag { get; set; }

    [JsonPropertyName("campaign_id")]
    public int CampaignId { get; set; }
}
