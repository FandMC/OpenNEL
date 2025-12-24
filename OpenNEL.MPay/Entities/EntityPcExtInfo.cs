using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntityPcExtInfo
{
    [JsonPropertyName("qr_expire_time")]
    public int QrExpireTime { get; set; }

    [JsonPropertyName("wps_token")]
    public string WpsToken { get; set; } = string.Empty;

    [JsonPropertyName("wps_refresh_token")]
    public string WpsRefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("wps_uid")]
    public string WpsUid { get; set; } = string.Empty;

    [JsonPropertyName("wps_nick_name")]
    public string WpsNickName { get; set; } = string.Empty;

    [JsonPropertyName("wps_avatar")]
    public string WpsAvatar { get; set; } = string.Empty;
}
