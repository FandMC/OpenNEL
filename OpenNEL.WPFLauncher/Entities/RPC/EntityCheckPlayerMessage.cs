using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.RPC;

public class EntityCheckPlayerMessage
{
    [JsonPropertyName("a")]
    public int Length { get; set; }

    [JsonPropertyName("b")]
    public string Message { get; set; } = string.Empty;
}
