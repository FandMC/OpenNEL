using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.NetGame;

public class EntityNetGameServerAddressRequest
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;
}
