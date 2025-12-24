using System.Text.Json.Serialization;

namespace OpenNEL.G79.Entities;

public class EntityQueryUserDetail
{
    [JsonPropertyName("version")]
    public Version Version { get; set; } = new(2, 0);
}
