using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.X19;

public class X19AuthenticationData
{
    [JsonPropertyName("sa_data")] public required string SaData { get; set; }

    [JsonPropertyName("sauth_json")] public required string AuthJson { get; set; }

    [JsonPropertyName("version")] public required X19AuthenticationVersion Version { get; set; }

    [JsonPropertyName("sdkuid")] public string? SdkUid { get; set; } = null;

    [JsonPropertyName("aid")] public required string Aid { get; set; }

    [JsonPropertyName("hasMessage")] public bool HasMessage { get; set; } = false;

    [JsonPropertyName("hasGmail")] public bool HasGmail { get; set; } = false;

    [JsonPropertyName("otp_token")] public required string OtpToken { get; set; }

    [JsonPropertyName("otp_pwd")] public string? OtpPwd { get; set; } = null;

    [JsonPropertyName("lock_time")] public int LockTime { get; set; } = 0;

    [JsonPropertyName("env")] public string? Env { get; set; } = null;

    [JsonPropertyName("min_engine_version")]
    public string? MinEngineVersion { get; set; } = null;

    [JsonPropertyName("min_patch_version")]
    public string? MinPatchVersion { get; set; } = null;

    [JsonPropertyName("verify_status")] public int VerifyStatus { get; set; } = 0;

    [JsonPropertyName("unisdk_login_json")]
    public string? UniSdkLoginJson { get; set; } = null;

    [JsonPropertyName("token")] public string? Token { get; set; } = null;

    [JsonPropertyName("is_register")] public bool IsRegister { get; set; } = true;

    [JsonPropertyName("entity_id")] public string? EntityId { get; set; } = null;
}

public class X19AuthenticationVersion
{
    [JsonPropertyName("version")] public required string Version { get; set; }

    [JsonPropertyName("launcher_md5")] public string LauncherMd5 { get; set; } = string.Empty;

    [JsonPropertyName("updater_md5")] public string UpdaterMd5 { get; set; } = string.Empty;
}