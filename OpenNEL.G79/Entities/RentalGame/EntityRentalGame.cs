using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.RentalGame;

public class EntityRentalGame
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("player_count")]
    public int PlayerCount { get; set; }

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;
}
