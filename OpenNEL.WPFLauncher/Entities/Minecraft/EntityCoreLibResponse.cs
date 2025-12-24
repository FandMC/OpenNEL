using System.Text.Json.Serialization;

namespace OpenNEL.WPFLauncher.Entities.Minecraft;

public class EntityCoreLibResponse
{
    [JsonPropertyName("core_lib_md5")]
    public string CoreLibMd5 { get; set; } = string.Empty;

    [JsonPropertyName("core_lib_name")]
    public string CoreLibName { get; set; } = string.Empty;

    [JsonPropertyName("core_lib_size")]
    public int CoreLibSize { get; set; }

    [JsonPropertyName("core_lib_url")]
    public string CoreLibUrl { get; set; } = string.Empty;

    [JsonPropertyName("mc_version")]
    public int McVersion { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("refresh_time")]
    public int RefreshTime { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("libs")]
    public List<EntityCoreLib> Libs { get; set; } = new();

    [JsonPropertyName("natives")]
    public List<EntityCoreLib> Natives { get; set; } = new();
}

public class EntityCoreLib
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
