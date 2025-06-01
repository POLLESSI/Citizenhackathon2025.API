using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CityzenHackathon2025.API.Tools
{
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss"; // Forçage du format ISO complet

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return default;

            // Essaie de parser en DateTime avec heure
            if (DateTime.TryParse(value, out var date))
                return date;

            throw new JsonException($"Unable to parse DateTime from '{value}'.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Toujours écrire avec l'heure même si elle est 00:00
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}
