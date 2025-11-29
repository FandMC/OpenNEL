using System.Text.Json.Serialization;

namespace OpenNEL.type;

public class SettingData
{
    [JsonPropertyName("themeMode")] public string ThemeMode { get; set; } = "image";
    [JsonPropertyName("themeColor")] public string ThemeColor { get; set; } = "#181818";
    [JsonPropertyName("themeImage")] public string ThemeImage { get; set; } = string.Empty;
}
