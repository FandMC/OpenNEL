using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.NetGame;

public class EntityComponentDownloadInfoResponseSub
{
    [JsonPropertyName("java_version")]
    public int JavaVersion { get; set; }

    [JsonPropertyName("mc_version_name")]
    public string McVersionName { get; set; } = string.Empty;

    [JsonPropertyName("res_url")]
    public string ResUrl { get; set; } = string.Empty;

    [JsonPropertyName("res_size")]
    public long ResSize { get; set; }

    [JsonPropertyName("res_md5")]
    public string ResMd5 { get; set; } = string.Empty;

    [JsonPropertyName("jar_md5")]
    public string JarMd5 { get; set; } = string.Empty;

    [JsonPropertyName("res_name")]
    public string ResName { get; set; } = string.Empty;

    [JsonPropertyName("res_version")]
    public int ResVersion { get; set; }
}
