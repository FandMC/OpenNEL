using System.Text.Json.Serialization;

namespace OpenNEL.GameLauncher.Entities;

public class EntityProgressUpdate
{
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

    [JsonPropertyName("percent")]
    public required int Percent { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }
}
