using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityRentalGame
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("visibility")]
    public EnumVisibilityStatus Visibility { get; set; }

    [JsonPropertyName("has_pwd")]
    public string HasPassword { get; set; } = string.Empty;

    [JsonPropertyName("server_type")]
    public EnumServerType ServerType { get; set; }

    [JsonPropertyName("status")]
    public EnumServerStatus Status { get; set; }

    [JsonPropertyName("capacity")]
    public uint Capacity { get; set; }

    [JsonPropertyName("mc_version")]
    public string McVersion { get; set; } = string.Empty;

    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    [JsonPropertyName("player_count")]
    public uint PlayerCount { get; set; }

    [JsonConverter(typeof(JsonConverters.SingleOrArrayConverter<string>))]
    [JsonPropertyName("image_url")]
    public List<string> ImageUrl { get; set; } = new();

    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = string.Empty;

    [JsonPropertyName("min_level")]
    public string MinLevel { get; set; } = string.Empty;

    [JsonPropertyName("pvp")]
    public bool IsPvpEnabled { get; set; }

    [JsonPropertyName("like_num")]
    public uint LikeCount { get; set; }

    [JsonPropertyName("icon_index")]
    public uint IconIndex { get; set; }

    [JsonPropertyName("offset")]
    public string? Offset { get; set; }
}
