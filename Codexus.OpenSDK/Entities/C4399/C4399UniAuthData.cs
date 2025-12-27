using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.C4399;

public class C4399UniAuthData
{
    [JsonPropertyName("ops")] public List<C4399OpsItem> Ops { get; set; } = [];

    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;

    [JsonPropertyName("login_tip")] public string LoginTip { get; set; } = string.Empty;

    [JsonPropertyName("sdk_login_data")] public string SdkLoginData { get; set; } = string.Empty;
}