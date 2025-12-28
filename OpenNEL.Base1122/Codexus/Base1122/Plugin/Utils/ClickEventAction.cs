using System.Text.Json.Serialization;

namespace Codexus.Base1122.Plugin.Utils;

[JsonConverter(typeof(MinecraftEnumConverter<ClickEventAction>))]
public enum ClickEventAction
{
	[JsonPropertyName("open_url")]
	OpenUrl,
	[JsonPropertyName("run_command")]
	RunCommand,
	[JsonPropertyName("suggest_command")]
	SuggestCommand,
	[JsonPropertyName("change_page")]
	ChangePage,
	[JsonPropertyName("copy_to_clipboard")]
	CopyToClipboard
}
