using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19Response
{
    [JsonPropertyName("code")] public int Code { get; set; } = 0;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}