using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Skin;

public class EntitySkinSettings
{
    [JsonPropertyName("client_type")]
    public string ClientType { get; set; } = string.Empty;

    [JsonPropertyName("game_type")]
    public int GameType { get; set; }

    [JsonPropertyName("skin_id")]
    public string SkinId { get; set; } = string.Empty;

    [JsonPropertyName("skin_mode")]
    public int SkinMode { get; set; }

    [JsonPropertyName("skin_type")]
    public int SkinType { get; set; }
}
