using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityX19CookieRequest
{
    [JsonPropertyName("sauth_json")]
    public string Json { get; set; } = string.Empty;
}
