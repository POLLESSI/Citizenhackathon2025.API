using System.Text.Json.Serialization;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models
{
    public sealed class OneCallResponse
    {
        [JsonPropertyName("lat")] public double Lat { get; set; }
        [JsonPropertyName("lon")] public double Lon { get; set; }

        // Current alerts are sufficient for your use case “alerts ingestion”.
        [JsonPropertyName("current")] public OneCallCurrent? Current { get; set; }
        [JsonPropertyName("alerts")] public List<OneCallAlert>? Alerts { get; set; }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.