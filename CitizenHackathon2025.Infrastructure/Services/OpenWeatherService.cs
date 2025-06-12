using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using Newtonsoft.Json;

namespace Citizenhackathon2025.Infrastructure.Services
{
    public class OpenWeatherService : IOpenWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenWeatherService> _logger;

        public OpenWeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenWeatherService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city)
        {
            var apiKey = _configuration["OpenWeather:ApiKey"];
            var baseUrl = _configuration["OpenWeather:BaseUrl"];
            var url = $"{baseUrl}/weather?q={city}&appid={apiKey}&units=metric";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeather request failed with status code: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new WeatherForecastDTO
                {
                    DateWeather = DateTime.UtcNow,
                    TemperatureC = root.GetProperty("main").GetProperty("temp").GetRawText(),
                    Summary = root.GetProperty("weather")[0].GetProperty("description").GetString(),
                    Humidity = root.GetProperty("main").GetProperty("humidity").GetRawText(),
                    RainfallMm = root.TryGetProperty("rain", out var rainProp) && rainProp.TryGetProperty("1h", out var rain1h)
                        ? rain1h.GetRawText()
                        : "0",
                    WindSpeedKmh = root.GetProperty("wind").GetProperty("speed").GetRawText()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data from OpenWeather");
                return null;
            }

        }

        public async Task<WeatherForecastDTO?> GetForecastAsync(string city)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid=YOUR_API_KEY&units=metric";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            // Exemple simplifié, il faudra adapter en fonction du DTO et de la réponse API
            var openWeatherResponse = System.Text.Json.JsonSerializer.Deserialize<OpenWeatherApiResponse>(json);

            // Mapper OpenWeatherApiResponse vers WeatherForecastDTO (à définir)
            var dto = new WeatherForecastDTO
            {
                DateWeather = DateTime.Now,
                TemperatureC = openWeatherResponse.Main.Temp.ToString(),
                Summary = openWeatherResponse.Weather.FirstOrDefault()?.Description ?? "No description"
            };

            return dto;
        }
        // Exemple de modèle pour désérialisation de l’API
        public class OpenWeatherApiResponse
        {
        #nullable disable
            public MainInfo Main { get; set; }
            public List<WeatherInfo> Weather { get; set; }
        }
        public class MainInfo
        {
            public float Temp { get; set; }
        }

        public class WeatherInfo
        {
            public string Description { get; set; }
        }
    }
}

