using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.MPay;

public class MPayParameters
{
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
    [JsonPropertyName("unique_id")] public string UniqueId { get; set; } = string.Empty;
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
}