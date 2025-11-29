using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.Role;

public class EntityCreateRoleRequest
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("name")]
	public required string RoleName { get; set; }

	[JsonPropertyName("game")]
	public required string GameId { get; set; }

	[JsonPropertyName("type")]
	public required string GameType { get; set; }
}
