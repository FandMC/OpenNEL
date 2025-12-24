using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities;

public class EntitySetNickName
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
