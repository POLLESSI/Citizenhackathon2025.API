using System.Net.Http.Json;
using CitizenHackathon2025.Application.Interfaces;       // ou DTOs si tu veux
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.Extensions.Configuration;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WazeTrafficApiService : ITrafficApiService
    {
        private readonly HttpClient _http;
        private readonly string _wazeEndpoint;
        private readonly string _authToken;

        public WazeTrafficApiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _wazeEndpoint = config["Waze:Endpoint"];
            _authToken = config["Waze:Token"]; 
        }
        public async Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude, CancellationToken ct = default)
        {
            // Example URL (to be adapted according to your Waze subscription)
            var url = $"{_wazeEndpoint}/traffic?lat={latitude}&lon={longitude}&token={_authToken}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            // It is assumed that the API returns a JSON object conforming to the DTO
            return await response.Content.ReadFromJsonAsync<TrafficConditionDTO>();
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.