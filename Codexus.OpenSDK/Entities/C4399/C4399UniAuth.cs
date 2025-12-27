using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.C4399;

public class C4399UniAuth
{
    [JsonPropertyName("code")] public int Code { get; set; } = 0;

    [JsonPropertyName("msg")] public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("data")] public C4399UniAuthData Data { get; set; } = new();
}