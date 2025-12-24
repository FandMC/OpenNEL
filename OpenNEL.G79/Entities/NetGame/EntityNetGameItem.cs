using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities.NetGame;

public class EntityNetGameItem
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("res_name")]
    public string ResName { get; set; } = string.Empty;

    [JsonPropertyName("brief")]
    public string Brief { get; set; } = string.Empty;

    [JsonPropertyName("tag_names")]
    public List<string> TagNames { get; set; } = new();

    [JsonPropertyName("title_image_url")]
    public string TitleImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("new_recommend")]
    public int NewRecommend { get; set; }

    [JsonPropertyName("new_entrance_recommend")]
    public int NewEntranceRecommend { get; set; }

    [JsonPropertyName("new_recommend_time")]
    public int NewRecommendTime { get; set; }

    [JsonPropertyName("order")]
    public string Order { get; set; } = string.Empty;

    [JsonPropertyName("is_spigot")]
    public int IsSpigot { get; set; }

    [JsonPropertyName("stars")]
    public float Stars { get; set; }

    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("online_num")]
    public string OnlineNum { get; set; } = string.Empty;

    [JsonPropertyName("pic_url_list")]
    public List<string> PicUrlList { get; set; } = new();

    [JsonPropertyName("is_access_by_uid")]
    public int IsAccessByUid { get; set; }

    [JsonPropertyName("opening_hour")]
    public string OpeningHour { get; set; } = string.Empty;

    [JsonPropertyName("sort_description")]
    public string SortDescription { get; set; } = string.Empty;

    [JsonPropertyName("is_show_online_count")]
    public int IsShowOnlineCount { get; set; }

    [JsonPropertyName("sort")]
    public int Sort { get; set; }

    [JsonPropertyName("is_fellow")]
    public int IsFellow { get; set; }

    [JsonPropertyName("developer_id")]
    public int DeveloperId { get; set; }

    [JsonPropertyName("friend_play_num")]
    public int FriendPlayNum { get; set; }

    [JsonPropertyName("week_play_num")]
    public int WeekPlayNum { get; set; }

    [JsonPropertyName("recommend_sort_num")]
    public int RecommendSortNum { get; set; }

    [JsonPropertyName("total_play_num")]
    public int TotalPlayNum { get; set; }

    [JsonPropertyName("create_time")]
    public int CreateTime { get; set; }

    [JsonPropertyName("running_status")]
    public string RunningStatus { get; set; } = string.Empty;
}
