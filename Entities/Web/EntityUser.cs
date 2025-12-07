using System.Text.Json.Serialization;
using OpenNEL.Enums;

namespace OpenNEL.Entities.Web;

public class EntityUser
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("authorized")]
	public required bool Authorized { get; set; }

	[JsonPropertyName("auto_login")]
	public required bool AutoLogin { get; set; }

	[JsonPropertyName("channel")]
	public required string Channel { get; set; }

	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("details")]
	public required string Details { get; set; }

	[JsonPropertyName("platform")]
	public Platform Platform { get; set; }

	[JsonPropertyName("alias")]
	public string Alias { get; set; } = string.Empty;
}
