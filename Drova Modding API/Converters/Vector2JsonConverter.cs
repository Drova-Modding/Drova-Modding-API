using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Drova_Modding_API.Converters
{
    /// <summary>
    /// Converter for <see cref="Vector2"/>, written as <c>"x;y"</c> in the invariant culture.
    /// Reading also accepts a decimal comma, for files written in another culture.
    /// </summary>
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        /// <inheritdoc/>
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            string[] parts = value?.Split(';') ?? [];
            if (parts.Length != 2 || !TryParseFloat(parts[0], out float x) || !TryParseFloat(parts[1], out float y))
            {
                throw new JsonException($"Expected a position \"x;y\" like \"10.5;-20\", got {(value == null ? reader.TokenType.ToString() : "\"" + value + "\"")}.");
            }
            return new Vector2(x, y);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.x.ToString("R", CultureInfo.InvariantCulture) + ";" + value.y.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}