using System.Text.Json.Serialization;

namespace Codexus.Base1122.Plugin.Utils;

public class HoverEvent
{
	[JsonPropertyName("action")]
	public HoverEventAction Action { get; set; }

	[JsonPropertyName("value")]
	public object? Value { get; set; }
}
