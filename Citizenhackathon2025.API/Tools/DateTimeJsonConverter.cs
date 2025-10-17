using System.Text.Json;
using System.Text.Json.Serialization;

namespace CitizenHackathon2025.API.Tools
{
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss"; // Force full ISO format

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return default;

            // Try parsing into DateTime with time
            if (DateTime.TryParse(value, out var date))
                return date;

            throw new JsonException($"Unable to parse DateTime from '{value}'.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Always write with the time even if it is 00:00
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}



























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.