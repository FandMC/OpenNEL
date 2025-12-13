using System.Text.Json.Serialization;

namespace OpenNEL.type;

public class SettingData
{
    [JsonPropertyName("themeMode")] public string ThemeMode { get; set; } = "image";
    [JsonPropertyName("themeColor")] public string ThemeColor { get; set; } = "#181818";
    [JsonPropertyName("themeImage")] public string ThemeImage { get; set; } = string.Empty;
    [JsonPropertyName("backdrop")] public string Backdrop { get; set; } = "mica";
    [JsonPropertyName("vetaProcessKeyword")] public string VetaProcessKeyword { get; set; } = "Veta";
    [JsonPropertyName("autoCopyIpOnStart")] public bool AutoCopyIpOnStart { get; set; } = false;
    [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
}
