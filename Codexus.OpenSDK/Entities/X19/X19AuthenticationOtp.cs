using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19AuthenticationOtp
{
    [JsonPropertyName("entity_id")] public string EntityId { get; set; } = "";

    [JsonPropertyName("account")] public string Account { get; set; } = "";

    [JsonPropertyName("token")] public string Token { get; set; } = "";

    [JsonPropertyName("sead")] public string Sead { get; set; } = "";

    [JsonPropertyName("hasMessage")] public bool HasMessage { get; set; } = false;

    [JsonPropertyName("aid")] public string Aid { get; set; } = "";

    [JsonPropertyName("sdkuid")] public string SdkUid { get; set; } = "";

    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";

    [JsonPropertyName("unisdk_login_json")]
    public string UniSdkLoginJson { get; set; } = "";

    [JsonPropertyName("verify_status")] public int VerifyStatus { get; set; }

    [JsonPropertyName("hasGmail")] public bool HasGmail { get; set; }

    [JsonPropertyName("is_register")] public bool IsRegister { get; set; }

    [JsonPropertyName("env")] public string Env { get; set; } = "";

    [JsonPropertyName("last_server_up_time")]
    public long LastServerUpTime { get; set; }

    [JsonPropertyName("min_engine_version")]
    public string MinEngineVersion { get; set; } = "";

    [JsonPropertyName("min_patch_version")]
    public string MinPatchVersion { get; set; } = "";
}