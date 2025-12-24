using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class Entity4399OAuthResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public Entity4399OAuthResult Result { get; set; } = new();
}
