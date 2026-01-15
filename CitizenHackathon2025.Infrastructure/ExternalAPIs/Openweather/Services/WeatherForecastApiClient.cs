using CitizenHackathon2025.DTOs.DTOs;
using System.Net.Http.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Services
{
    public sealed class WeatherForecastApiClient
    {
        private readonly HttpClient _http;
        public WeatherForecastApiClient(HttpClient http) => _http = http;

        public Task<List<WeatherForecastDTO>?> GetCurrentAsync(CancellationToken ct = default)
            => _http.GetFromJsonAsync<List<WeatherForecastDTO>>("api/WeatherForecast/current", ct);

        public async Task PullAsync(decimal lat, decimal lon, CancellationToken ct = default)
        {
            var url = $"api/WeatherForecast/pull?lat={lat}&lon={lon}";
            using var resp = await _http.PostAsync(url, null, ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}




























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.