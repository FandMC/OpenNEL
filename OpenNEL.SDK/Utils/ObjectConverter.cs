using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.SDK.Utils;

public class ObjectConverter : JsonConverter<object>
{
	public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.True:
			return true;
		case JsonTokenType.False:
			return false;
		case JsonTokenType.String:
			return reader.GetString();
		case JsonTokenType.Number:
		{
			if (reader.TryGetInt32(out var value))
			{
				return value;
			}
			if (reader.TryGetInt64(out var value2))
			{
				return value2;
			}
			return reader.GetDouble();
		}
		case JsonTokenType.Null:
			return null;
		case JsonTokenType.StartObject:
			return JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
		case JsonTokenType.StartArray:
			return JsonSerializer.Deserialize<List<object>>(ref reader, options);
		default:
			return JsonSerializer.Deserialize<JsonElement>(ref reader).GetObject();
		}
	}

	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
	}
}
