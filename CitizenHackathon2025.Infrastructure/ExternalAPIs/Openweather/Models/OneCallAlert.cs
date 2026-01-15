using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models
{
    public sealed class OneCallAlert
    {
        [JsonPropertyName("sender_name")] public string? SenderName { get; set; }
        [JsonPropertyName("event")] public string? Event { get; set; }

        // Unix seconds
        [JsonPropertyName("start")] public long Start { get; set; }
        [JsonPropertyName("end")] public long End { get; set; }

        [JsonPropertyName("description")] public string? Description { get; set; }

        // sometimes present according to sources
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    }
}



































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.