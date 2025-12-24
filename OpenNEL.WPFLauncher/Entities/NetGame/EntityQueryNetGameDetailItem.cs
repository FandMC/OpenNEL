using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityQueryNetGameDetailItem
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title_image_url")]
    public string TitleImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("online_count")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string OnlineCount { get; set; } = string.Empty;

    [JsonPropertyName("total_play_count")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string TotalPlayCount { get; set; } = string.Empty;

    [JsonPropertyName("star")]
    public double Star { get; set; }

    [JsonPropertyName("mc_version")]
    public int McVersion { get; set; }

    [JsonPropertyName("brief_image_urls")]
    public string[] BriefImageUrls { get; set; } = Array.Empty<string>();

    [JsonPropertyName("detail_description")]
    public string DetailDescription { get; set; } = string.Empty;

    [JsonPropertyName("developer_name")]
    public string DeveloperName { get; set; } = string.Empty;

    [JsonPropertyName("developer_urs")]
    public string DeveloperUrs { get; set; } = string.Empty;

    [JsonPropertyName("publish_time")]
    public int PublishTime { get; set; }

    [JsonPropertyName("video_info_list")]
    public EntityDetailsVideo[] VideoInfoList { get; set; } = Array.Empty<EntityDetailsVideo>();

    [JsonPropertyName("mc_version_list")]
    public EntityMcVersion[] McVersionList { get; set; } = Array.Empty<EntityMcVersion>();

    [JsonPropertyName("server_address")]
    public string ServerAddress { get; set; } = string.Empty;

    [JsonPropertyName("server_port")]
    public int ServerPort { get; set; }
}
