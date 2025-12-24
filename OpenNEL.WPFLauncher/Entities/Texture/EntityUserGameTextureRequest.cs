using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Texture;

public class EntityUserGameTextureRequest
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("game_type")]
    public int GameType { get; set; }

    [JsonPropertyName("skin_type")]
    public int SkinType { get; set; }

    [JsonPropertyName("client_type")]
    public string ClientType { get; set; } = "java";

    [JsonPropertyName("length")]
    public int Length { get; set; } = 20;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}
