using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntitySmsTicket
{
    [JsonPropertyName("ticket")]
    public string Ticket { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
