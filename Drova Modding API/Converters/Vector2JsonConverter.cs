using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Drova_Modding_API.Converters
{
    /// <summary>
    /// Custom Converter for <see cref="Vector2"/>
    /// </summary>
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        /// <inheritdoc/>
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return Vector2.zero;
            
            var parts = value.Split(';');
            return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.x};{value.y}");
        }
    }
}