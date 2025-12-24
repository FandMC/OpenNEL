using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RentalGame;

public class EntityRentalGameDetails
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("owner_id")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("brief_summary")]
    public string BriefSummary { get; set; } = string.Empty;

    [JsonPropertyName("icon_index")]
    public uint IconIndex { get; set; }

    [JsonPropertyName("begin_time")]
    public ulong BeginTime { get; set; }

    [JsonPropertyName("mc_version")]
    public string McVersion { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public uint Capacity { get; set; }

    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = string.Empty;

    [JsonPropertyName("player_count")]
    public uint PlayerCount { get; set; }

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public EnumServerStatus Status { get; set; }

    [JsonPropertyName("visibility")]
    public EnumVisibilityStatus Visibility { get; set; }

    [JsonPropertyName("has_pwd")]
    public string HasPassword { get; set; } = string.Empty;

    [JsonPropertyName("server_type")]
    public EnumServerType ServerType { get; set; }

    [JsonPropertyName("active_components")]
    public List<string>? ActiveComponents { get; set; } = new();

    [JsonPropertyName("update_active_components")]
    public List<string>? UpdateActiveComponents { get; set; } = new();

    [JsonPropertyName("server_ip")]
    public string? ServerIp { get; set; }

    [JsonPropertyName("server_port")]
    public int? ServerPort { get; set; }
}
