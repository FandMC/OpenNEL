using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities;

public class EntitySetNickNameRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;
}
