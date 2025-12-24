using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.RentalGame;

public class EntityRentalGameServerAddressRequest
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("pwd")]
    public string Password { get; set; } = string.Empty;
}
