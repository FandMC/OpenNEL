using System.Text.Json.Serialization;

namespace OpenNEL.GameLauncher.Connection.Entities;

public class EntityHandshake
{
    [JsonPropertyName("handshakeBody")]
    public string HandshakeBody { get; set; } = string.Empty;
}
