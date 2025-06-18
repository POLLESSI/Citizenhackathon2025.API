
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.Services
{
    public class TrafficAPIService : ITrafficApiService
    {
        private readonly HttpClient _httpClient;

        public TrafficAPIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude)
        {
            // Simulates a call to Waze or other API
            var dto = new TrafficConditionDTO
            {
                Latitude = latitude.ToString(),
                Longitude = longitude.ToString(),
                DateCondition = DateTime.UtcNow,
                CongestionLevel = "4",
                IncidentType = "Accident"
            };
            return Task.FromResult<TrafficConditionDTO?>(dto); ;
        }
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.