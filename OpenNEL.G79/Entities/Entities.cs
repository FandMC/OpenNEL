using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;
using OpenNEL.WPFLauncher.Entities;

namespace OpenNEL.G79.Entities;

public class Entities<T> : EntityResponse
{
    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    [JsonPropertyName("entity")]
    public T? Data { get; set; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string Total { get; set; } = "0";
}
