using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityPluginsResponse
{
	[JsonPropertyName("id")]
	public required string PluginId { get; set; }

	[JsonPropertyName("name")]
	public required string PluginName { get; set; }

	[JsonPropertyName("description")]
	public required string PluginDescription { get; set; }

	[JsonPropertyName("version")]
	public required string PluginVersion { get; set; }

	[JsonPropertyName("author")]
	public required string PluginAuthor { get; set; }

	[JsonPropertyName("status")]
	public required string PluginStatus { get; set; }

	[JsonPropertyName("waiting_restart")]
	public required bool PluginWaitingRestart { get; set; }
}
