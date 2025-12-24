using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities;

public class EntityAuthenticationOtp
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
