using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityQueryInstall
{
	[JsonPropertyName("id")]
	public string PluginId { get; set; } = string.Empty;

	[JsonPropertyName("version")]
	public string PluginVersion { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public bool PluginIsInstalled { get; set; }

	[JsonPropertyName("installed")]
	public string PluginInstalledVersion { get; set; } = string.Empty;

	[JsonPropertyName("restart")]
	public bool PluginIsWaitingRestart { get; set; }
}
