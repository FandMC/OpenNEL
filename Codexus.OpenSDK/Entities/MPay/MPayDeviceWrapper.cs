using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.MPay;

public class MPayDeviceWrapper
{
    [JsonPropertyName("device")] public MPayDevice Device { get; set; } = new();
}