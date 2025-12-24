using System.Text.Json.Serialization;

namespace OpenNEL.MPay.Entities;

public class EntityUsersParameters
{
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("unique_id")]
    public string Unique { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}
