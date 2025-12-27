using System.Text.Json.Serialization;

namespace Codexus.OpenSDK.Entities.MPay;

public class MPaySMSTicket
{
    [JsonPropertyName("guide_text")] public string GuideText { get; set; } = string.Empty;
    [JsonPropertyName("related_emails")] public string[] RelatedEmails { get; set; } = [];
    [JsonPropertyName("ticket")] public string Ticket { get; set; } = string.Empty;
    [JsonPropertyName("related_accounts")] public string[] RelatedAccounts { get; set; } = [];
}