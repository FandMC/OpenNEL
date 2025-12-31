using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.Core.Utils;

public class TextComponent
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("translate")]
    public string Translate { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("bold")]
    public bool Bold { get; set; } = false;

    [JsonPropertyName("extra")]
    public List<TextComponent> Extra { get; set; } = new();

    [JsonIgnore]
    public string FullText { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayText => !string.IsNullOrEmpty(FullText) ? FullText 
        : !string.IsNullOrEmpty(Text) ? Text : Translate;

    public string ToJson() => JsonSerializer.Serialize(this);
}
