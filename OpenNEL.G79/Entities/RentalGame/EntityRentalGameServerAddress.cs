using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.RentalGame;

public class EntityRentalGameServerAddress
{
    [JsonPropertyName("mcserver_host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("mcserver_port")]
    public int Port { get; set; }
}
