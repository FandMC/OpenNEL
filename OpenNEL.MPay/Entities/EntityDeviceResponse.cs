using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntityDeviceResponse
{
    [JsonPropertyName("device")]
    public EntityDevice Device { get; set; } = new();
}
