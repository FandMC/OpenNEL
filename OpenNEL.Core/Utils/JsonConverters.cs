using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.Core.Utils;

public static class JsonConverters
{
    public class NetEaseIntConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
            {
                var text = reader.GetString();
                return text ?? string.Empty;
            }
            return reader.GetInt32().ToString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public class SingleOrArrayConverter<T> : JsonConverter<List<T>>
    {
        public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions? options)
        {
            var list = new List<T>();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                var item = JsonSerializer.Deserialize<T>(ref reader, options);
                if (item != null) list.Add(item);
                return list;
            }
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var item = JsonSerializer.Deserialize<T>(ref reader, options);
                if (item != null) list.Add(item);
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }

    public class StringFromNumberOrStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64().ToString(),
                JsonTokenType.String => reader.GetString(),
                _ => throw new JsonException("Unsupported token type for string conversion.")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
