using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CitizenHackathon2025.Application.Services
{
    public class OpenWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenWeatherService> _logger;

        public OpenWeatherService(HttpClient httpClient, IConfiguration config, ILogger<OpenWeatherService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<WeatherInfoDTO?> GetWeatherAsync(string city)
        {
            try
            {
                var apiKey = _config["OpenWeather:ApiKey"];
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

                var response = await _httpClient.GetFromJsonAsync<dynamic>(url);
                if (response == null) return null;

                return new WeatherInfoDTO
                {
                    Location = response.name,
                    TemperatureCelsius = (double)response.main.temp,
                    WeatherDescription = (string)response.weather[0].description,
                    WindSpeedKmh = (double)response.wind.speed * 3.6,
                    HumidityPercent = (double)response.main.humidity,
                    RetrievedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weather");
                return null;
            }
        }
    }
}
