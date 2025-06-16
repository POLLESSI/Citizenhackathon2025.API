using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Shared;
using Citizenhackathon2025.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Application.Services
{
    public class OpenWeatherService : IOpenWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenWeatherService> _logger;
        private readonly OpenWeatherOptions _options;
        private readonly string _apiKey;

        public OpenWeatherService(HttpClient httpClient, IConfiguration config, ILogger<OpenWeatherService> logger, OpenWeatherOptions options, string apiKey)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _options = options;
            _apiKey = apiKey;
        }

        public Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city)
        {
            throw new NotImplementedException();
        }

        public Task<WeatherForecastDTO?> GetForecastAsync(string city)
        {
            throw new NotImplementedException();
        }

        public async Task<WeatherForecastDTO> GetWeatherAsync(double lat, double lon)
        {
            try
            {
                var url = $"{_options.BaseUrl}/data/2.5/weather?lat={lat}&lon={lon}&appid={_options.ApiKey}&units=metric&lang=fr";
                var response = await _httpClient.GetFromJsonAsync<WeatherForecastDTO>(url);
                return response ?? throw new Exception("Réponse vide");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'appel à l'API météo");
                throw;
            }
        }

        public async Task<string> GetWeatherSummaryAsync(string location)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={location}&units=metric&lang=fr&appid={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return $"Unable to retrieve weather for {location}.";

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
            var weather = root.GetProperty("weather")[0].GetProperty("description").GetString();
            var wind = root.GetProperty("wind").GetProperty("speed").GetDouble();

            return $"he does {temp}°C with a time {weather} and a wind of {wind} m/s";
        }
    }
}
