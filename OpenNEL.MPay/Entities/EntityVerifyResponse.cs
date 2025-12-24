using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntityVerifyResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
