using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;

namespace OpenNEL.WPFLauncher.Entities;

public class Entities<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("entities")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("total")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string Total { get; set; } = "0";
}
