using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityQueryGameSessions
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("server_name")]
	public required string ServerName { get; set; }

	[JsonPropertyName("guid")]
	public required string Guid { get; set; }

	[JsonPropertyName("character_name")]
	public required string CharacterName { get; set; }

	[JsonPropertyName("server_version")]
	public required string ServerVersion { get; set; }

	[JsonPropertyName("status_text")]
	public required string StatusText { get; set; }

	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("progress_value")]
	public required int ProgressValue { get; set; }

	[JsonPropertyName("local_address")]
	public string LocalAddress { get; set; } = string.Empty;
}
