using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19Entity<T> : X19Response
{
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
    [JsonPropertyName("entity")] public T? Data { get; set; } = default;
}