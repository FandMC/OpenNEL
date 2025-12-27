using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19SAuthJsonWrapper
{
    [JsonPropertyName("sauth_json")] public required string Json { get; set; }
}