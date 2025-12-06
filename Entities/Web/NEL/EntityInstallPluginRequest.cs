using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityInstallPluginRequest
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("plugin")]
	public required EntityInstallPlugin Plugin { get; set; }
}
