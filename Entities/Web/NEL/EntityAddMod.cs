using System.Text.Json.Serialization;

namespace OpenNEL.Entities.Web.NEL;

public class EntityAddMod
{
	[JsonPropertyName("file_name")]
	public string FileName { get; set; } = string.Empty;

	[JsonPropertyName("base64")]
	public string Base64 { get; set; } = string.Empty;
}
