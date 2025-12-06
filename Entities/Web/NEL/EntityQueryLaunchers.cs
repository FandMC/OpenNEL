using System;
using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityQueryLaunchers
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("name")]
	public required Guid Name { get; set; }

	[JsonPropertyName("role")]
	public required string Role { get; set; }

	[JsonPropertyName("server")]
	public required string Server { get; set; }

	[JsonPropertyName("version")]
	public required string Version { get; set; }

	[JsonPropertyName("status")]
	public required string StatusMessage { get; set; }

	[JsonPropertyName("progress")]
	public required int Progress { get; set; }

	[JsonPropertyName("process_id")]
	public required int ProcessId { get; set; }
}
