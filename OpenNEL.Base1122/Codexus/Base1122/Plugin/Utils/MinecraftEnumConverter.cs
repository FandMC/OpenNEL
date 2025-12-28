using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Codexus.Base1122.Plugin.Utils;

public class MinecraftEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
	private readonly Dictionary<TEnum, string> _enumToString = new Dictionary<TEnum, string>();

	private readonly Dictionary<string, TEnum> _stringToEnum = new Dictionary<string, TEnum>();

	public MinecraftEnumConverter()
	{
		TEnum[] values = Enum.GetValues<TEnum>();
		for (int i = 0; i < values.Length; i++)
		{
			TEnum val = values[i];
			JsonPropertyNameAttribute customAttribute = typeof(TEnum).GetMember(val.ToString())[0].GetCustomAttribute<JsonPropertyNameAttribute>();
			if (customAttribute != null)
			{
				_enumToString[val] = customAttribute.Name;
				_stringToEnum[customAttribute.Name] = val;
			}
			else
			{
				string text = ToSnakeCase(val.ToString());
				_enumToString[val] = text;
				_stringToEnum[text] = val;
			}
		}
	}

	public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string stringValue = reader.GetString();
		if (stringValue == null)
		{
			throw new JsonException("Cannot read enum value from null string.");
		}
		if (_stringToEnum.TryGetValue(stringValue, out var value))
		{
			return value;
		}
		using (IEnumerator<KeyValuePair<string, TEnum>> enumerator = _stringToEnum.Where<KeyValuePair<string, TEnum>>((KeyValuePair<string, TEnum> kvp) => string.Equals(kvp.Key, stringValue, StringComparison.OrdinalIgnoreCase)).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current.Value;
			}
		}
		if (Enum.TryParse<TEnum>(stringValue, ignoreCase: true, out var result))
		{
			return result;
		}
		throw new JsonException("Cannot parse enum value from string '" + stringValue + "'.");
	}

	public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(_enumToString.TryGetValue(value, out string value2) ? value2 : ToSnakeCase(value.ToString()));
	}

	private static string ToSnakeCase(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 10);
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsUpper(c) && i > 0)
			{
				stringBuilder.Append('_');
			}
			stringBuilder.Append(char.ToLowerInvariant(c));
		}
		return stringBuilder.ToString();
	}
}
