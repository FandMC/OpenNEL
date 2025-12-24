using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityQueryRentalGameServerAddress
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("pwd")]
    public string Password { get; set; } = string.Empty;
}
