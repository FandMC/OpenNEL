using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class Entity4399VipInfo
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("expire_time")]
    public long ExpireTime { get; set; }
}
