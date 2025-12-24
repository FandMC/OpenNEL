using System.Text.Json.Serialization;

namespace OpenNEL.Pc4399.Entities;

public class Entity4399Response
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Entity4399ResponseData Data { get; set; } = new();
}
