using System.Net.Http.Json;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Shared.DTOs;        // ou DTOs si tu veux
using Microsoft.Extensions.Configuration;

namespace Citizenhackathon2025.Application.Services
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

        public async Task<TrafficConditionDTO?> GetCurrentTrafficAsync(double latitude, double longitude)
        {
            // Exemple d'URL (à adapter selon ton abonnement Waze)
            var url = $"{_wazeEndpoint}/traffic?lat={latitude}&lon={longitude}&token={_authToken}";

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            // On suppose que l'API renvoie un objet JSON conforme au DTO
            return await response.Content.ReadFromJsonAsync<TrafficConditionDTO>();
        }
    }
}
