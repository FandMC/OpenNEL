using System;
using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityQueryInterceptors
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("name")]
	public required Guid Name { get; set; }

	[JsonPropertyName("address")]
	public required string Address { get; set; }

	[JsonPropertyName("role")]
	public required string Role { get; set; }

	[JsonPropertyName("server")]
	public required string Server { get; set; }

	[JsonPropertyName("version")]
	public required string Version { get; set; }

	[JsonPropertyName("local")]
	public required string LocalAddress { get; set; }
}
