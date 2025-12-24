using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.SDK.Entities;

public class TextComponent
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    public string ToJson() => JsonSerializer.Serialize(this);
}
