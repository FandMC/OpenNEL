using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.NetGame;

public class EntityNetGameServerAddress
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }
}
