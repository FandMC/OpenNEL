using System.Text.Json.Serialization;

namespace OpenNEL.Pc4399.Entities;

public record Entity4399Account
{
    [JsonPropertyName("account")]
    public string Account { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
