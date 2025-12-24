using System.Text.Json.Serialization;

namespace OpenNEL.Com4399.Entities;

public class Entity4399OAuthResult
{
    [JsonPropertyName("login_url")]
    public string LoginUrl { get; set; } = string.Empty;

    [JsonPropertyName("register_url")]
    public string RegisterUrl { get; set; } = string.Empty;
}
