using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.MPay;

public class MPayUserWrapper
{
    [JsonPropertyName("force_pwd")] public bool ForcePwd { get; set; } = false;
    [JsonPropertyName("verify_status")] public MPayVerifyStatus? VerifyStatus { get; set; }
    [JsonPropertyName("user")] public required MPayUser User { get; set; } = new();
}