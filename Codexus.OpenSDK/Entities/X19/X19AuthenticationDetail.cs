using System.Text.Json.Serialization;
using Codexus.OpenSDK.Generator;

namespace Codexus.OpenSDK.Entities.X19;

public class X19AuthenticationDetail
{
    [JsonPropertyName("os_name")] public string OsName { get; set; } = "windows";

    [JsonPropertyName("os_ver")] public string OsVer { get; set; } = "Microsoft Windows 11 专业版";

    [JsonPropertyName("mac_addr")] public string MacAddr { get; set; } = "";

    [JsonPropertyName("udid")] public required string Udid { get; set; }

    [JsonPropertyName("app_ver")] public required string AppVersion { get; set; }

    [JsonPropertyName("sdk_ver")] public string SdkVersion { get; set; } = "";

    [JsonPropertyName("network")] public string Network { get; set; } = "";

    [JsonPropertyName("disk")] public string Disk { get; set; } = StringGenerator.GenerateHexString(4).ToUpper();

    [JsonPropertyName("is64bit")] public string Is64Bit { get; set; } = "1";

    [JsonPropertyName("launcher_type")] public string LauncherType { get; set; } = "PC_java";

    [JsonPropertyName("pay_channel")] public required string PayChannel { get; set; }
}