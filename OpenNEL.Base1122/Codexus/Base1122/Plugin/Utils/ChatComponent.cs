using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codexus.Base1122.Plugin.Utils;

public class ChatComponent
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = "";

	[JsonPropertyName("color")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Color { get; set; }

	[JsonPropertyName("bold")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Bold { get; set; }

	[JsonPropertyName("italic")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Italic { get; set; }

	[JsonPropertyName("underlined")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Underlined { get; set; }

	[JsonPropertyName("strikethrough")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Strikethrough { get; set; }

	[JsonPropertyName("obfuscated")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Obfuscated { get; set; }

	[JsonPropertyName("insertion")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Insertion { get; set; }

	[JsonPropertyName("clickEvent")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ClickEvent? ClickEvent { get; set; }

	[JsonPropertyName("hoverEvent")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public HoverEvent? HoverEvent { get; set; }

	[JsonPropertyName("extra")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ChatComponent>? Extra { get; set; }

	public static ChatComponent Of(string text)
	{
		return new ChatComponent
		{
			Text = text
		};
	}

	public ChatComponent WithColor(string color)
	{
		Color = color;
		return this;
	}

	public ChatComponent WithBold(bool bold = true)
	{
		Bold = bold;
		return this;
	}

	public ChatComponent WithItalic(bool italic = true)
	{
		Italic = italic;
		return this;
	}

	public ChatComponent WithUnderlined(bool underlined = true)
	{
		Underlined = underlined;
		return this;
	}

	public ChatComponent WithClickEvent(ClickEventAction action, string value)
	{
		ClickEvent = new ClickEvent
		{
			Action = action,
			Value = value
		};
		return this;
	}

	public ChatComponent WithHoverEvent(HoverEventAction action, ChatComponent value)
	{
		HoverEvent = new HoverEvent
		{
			Action = action,
			Value = value
		};
		return this;
	}

	public ChatComponent WithHoverText(string text)
	{
		HoverEvent = new HoverEvent
		{
			Action = HoverEventAction.ShowText,
			Value = Of(text)
		};
		return this;
	}

	public ChatComponent AddExtra(ChatComponent component)
	{
		if (Extra == null)
		{
			List<ChatComponent> list = (Extra = new List<ChatComponent>());
		}
		Extra.Add(component);
		return this;
	}

	public string ToJson()
	{
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = false,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		return JsonSerializer.Serialize(this, options);
	}

	public static ChatComponent? FromJson(string json)
	{
		return JsonSerializer.Deserialize<ChatComponent>(json);
	}
}
