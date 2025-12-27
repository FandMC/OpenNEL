using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19PatchVersion
{
    [JsonPropertyName("size")] public long Size { get; set; } = 0;
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("md5")] public string Md5 { get; set; } = "";
}