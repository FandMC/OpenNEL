using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityQueryGameCharacters
{
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; } = 10;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("game_type")]
    public string GameType { get; set; } = "2";
}
