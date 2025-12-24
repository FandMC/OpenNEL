using System.Text.Json.Serialization;

namespace OpenNEL.Pc4399.Entities;

public class Entity4399ResponseData
{
    [JsonPropertyName("ops")]
    public List<Entity4399OpsItem> Ops { get; set; } = new();

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("login_tip")]
    public string LoginTip { get; set; } = string.Empty;

    [JsonPropertyName("sdk_login_data")]
    public string SdkLoginData { get; set; } = string.Empty;
}
