using System.Text.Json.Serialization;

namespace OpenNEL.SDK.Entities;

public class EntityAvailableUser
{
	[JsonPropertyName("id")]
	public required string UserId { get; set; }

	[JsonPropertyName("token")]
	public required string AccessToken { get; set; }

	[JsonPropertyName("last_login_time")]
	public required long LastLoginTime { get; set; }
}
