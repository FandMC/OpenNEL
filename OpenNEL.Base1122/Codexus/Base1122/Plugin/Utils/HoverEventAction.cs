using System.Text.Json.Serialization;

namespace Codexus.Base1122.Plugin.Utils;

[JsonConverter(typeof(MinecraftEnumConverter<HoverEventAction>))]
public enum HoverEventAction
{
	[JsonPropertyName("show_text")]
	ShowText,
	[JsonPropertyName("show_item")]
	ShowItem,
	[JsonPropertyName("show_entity")]
	ShowEntity
}
