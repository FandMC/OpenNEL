using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities;

public class Entity<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("entity")]
    public T? Data { get; set; }
}
