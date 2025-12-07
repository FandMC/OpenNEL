using System.Text.Json.Serialization;
using OpenNEL.Enums;

namespace OpenNEL.Entities.Web;

public class EntityLoginRequest
{
	[JsonPropertyName("channel")]
	public string Channel { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("details")]
	public string Details { get; set; } = string.Empty;

	[JsonPropertyName("platform")]
	public Platform Platform { get; set; }

	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;
}
