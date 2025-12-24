using System.Text.Json.Serialization;

namespace OpenNEL.SDK.Entities;

public class BodyIn
{
	[JsonPropertyName("body")]
	public required string Body { get; set; }
}
