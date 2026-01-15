using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models
{
    public sealed class OneCallWeather
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("main")] public string? Main { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.