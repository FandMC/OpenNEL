using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntityVerifyStatus
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;
}
