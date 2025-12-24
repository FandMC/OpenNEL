using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntityDevice
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}
