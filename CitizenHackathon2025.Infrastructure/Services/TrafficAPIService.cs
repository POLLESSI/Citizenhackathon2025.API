using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class TrafficAPIService : ITrafficApiService
    {
        private readonly HttpClient _httpClient;

        public TrafficAPIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude, CancellationToken ct = default)
        {
            // MOCK EXAMPLE(typically to be removed later):
            return new TrafficConditionDTO
            {
                Latitude = (decimal)latitude,
                Longitude = (decimal)longitude,
                DateCondition = DateTime.UtcNow,
                CongestionLevel = "4",
                IncidentType = "Accident"
            };

            // REAL CALL EXAMPLE (fictitious diagram)
            // var url = $"traffic?lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}";
            // var dto = await _http.GetFromJsonAsync<TrafficConditionDTO>(url, JsonDefaults.Options, ct);
            // return dto;
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.