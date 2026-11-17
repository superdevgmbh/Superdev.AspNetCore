using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superdev.AspNetCore
{
    /// <summary>
    /// Serializes <see cref="Version"/> values as JSON strings.
    /// </summary>
    public sealed class VersionJsonConverter : JsonConverter<Version>
    {
        /// <inheritdoc />
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Version values must be JSON strings.");
            }

            var value = reader.GetString();
            if (!Version.TryParse(value, out var version))
            {
                throw new JsonException($"'{value}' is not a valid Semantic Versioning 2.0.0 value.");
            }

            return version;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);

            writer.WriteStringValue(value.ToString());
        }
    }
}
