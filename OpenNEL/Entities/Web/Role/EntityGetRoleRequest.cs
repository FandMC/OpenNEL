using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.Role;

public class EntityGetRoleRequest
{
	[JsonPropertyName("id")]
	public string UserId { get; set; } = string.Empty;

	[JsonPropertyName("game")]
	public string GameId { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public string GameType { get; set; } = string.Empty;
}
