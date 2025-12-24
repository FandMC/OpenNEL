using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class Entity4399UserInfoResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public Entity4399UserInfoResult Result { get; set; } = new();
}
