using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityComponentDownloadInfoResponse
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("itype")]
    public int IType { get; set; }

    [JsonPropertyName("mtypeid")]
    public int MTypeId { get; set; }

    [JsonPropertyName("stypeid")]
    public int STypeId { get; set; }

    [JsonPropertyName("sub_entities")]
    public List<EntityComponentDownloadInfoResponseSub> SubEntities { get; set; } = new();

    [JsonPropertyName("sub_mod_list")]
    public List<ulong> SubModList { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = string.Empty;
}
