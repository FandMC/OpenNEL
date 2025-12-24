using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.NetGame;

public class EntityNetGameRequest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("channel_id")]
    public int ChannelId { get; set; }
}
