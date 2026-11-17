using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superdev.AspNetCore
{
    public sealed class LocalizedStringJsonConverter : JsonConverter<LocalizedString>
    {
        public override LocalizedString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("LocalizedString must be a JSON object.");
            }

            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
            var localizedString = dict != null
                ? new LocalizedString(dict)
                : new LocalizedString();
            return localizedString;
        }

        public override void Write(Utf8JsonWriter writer, LocalizedString value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var (key, localizedValue) in value)
            {
                writer.WriteString(key, localizedValue);
            }

            if (value.FallbackValue != null)
            {
                writer.WriteString(LocalizedString.FallbackJsonPropertyName, value.FallbackValue);
            }

            writer.WriteEndObject();
        }
    }
}
