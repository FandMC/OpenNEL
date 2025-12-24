using System.Text.Json.Serialization;
using OpenNEL.Core.Utils;

namespace OpenNEL.WPFLauncher.Entities;

public class EntityAuthenticationUpdate
{
    [JsonPropertyName("sa_data")]
    public object? SaData { get; set; }

    [JsonPropertyName("sauth_json")]
    public object? SauthJson { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("sdkuid")]
    public string? SdkUid { get; set; }

    [JsonPropertyName("aid")]
    [JsonConverter(typeof(JsonConverters.NetEaseIntConverter))]
    public string? Aid { get; set; }

    [JsonPropertyName("hasMessage")]
    public bool HasMessage { get; set; }

    [JsonPropertyName("hasGmail")]
    public bool HasGmail { get; set; }

    [JsonPropertyName("otp_token")]
    public string? OtpToken { get; set; }

    [JsonPropertyName("otp_pwd")]
    public string? OtpPwd { get; set; }

    [JsonPropertyName("lock_time")]
    public long LockTime { get; set; }

    [JsonPropertyName("env")]
    public string? Env { get; set; }

    [JsonPropertyName("min_engine_version")]
    public string? MinEngineVersion { get; set; }

    [JsonPropertyName("min_patch_version")]
    public string? MinPatchVersion { get; set; }

    [JsonPropertyName("verify_status")]
    public int VerifyStatus { get; set; }

    [JsonPropertyName("unisdk_login_json")]
    public object? UniSdkLoginJson { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("is_register")]
    public bool IsRegister { get; set; } = true;

    [JsonPropertyName("entity_id")]
    public string? EntityId { get; set; }
}
