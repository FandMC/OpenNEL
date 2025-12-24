using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class Entity4399UserInfoResult
{
    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    [JsonPropertyName("uname")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("nick")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("realname_status")]
    public int RealNameStatus { get; set; }

    [JsonPropertyName("realname_type")]
    public int RealNameType { get; set; }

    [JsonPropertyName("safe_level")]
    public int SafeLevel { get; set; }

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    [JsonPropertyName("vip")]
    public Entity4399VipInfo? Vip { get; set; }
}
