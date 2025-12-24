using System.Text.Json.Serialization;
using OpenNEL.WPFLauncher.Entities.NetGame.Texture;

namespace OpenNEL.WPFLauncher.Entities.Texture;

public class EntityUserGameTexture
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("game_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnumGType GameType { get; set; }

    [JsonPropertyName("skin_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnumTextureType SkinType { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("skin_id")]
    public string SkinId { get; set; } = string.Empty;

    [JsonPropertyName("skin_mode")]
    public int SkinMode { get; set; }

    [JsonPropertyName("client_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnumGameClientType ClientType { get; set; }

    [JsonPropertyName("title_image_url")]
    public string TitleImageUrl { get; set; } = string.Empty;
}
