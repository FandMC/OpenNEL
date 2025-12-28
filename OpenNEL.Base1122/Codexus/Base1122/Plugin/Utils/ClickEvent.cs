using System.Text.Json.Serialization;

namespace Codexus.Base1122.Plugin.Utils;

public class ClickEvent
{
	[JsonPropertyName("action")]
	public ClickEventAction Action { get; set; }

	[JsonPropertyName("value")]
	public string? Value { get; set; }
}
