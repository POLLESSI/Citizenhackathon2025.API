using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.Services
{
    public class OpenWeatherMapService : IOpenWeatherMapService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenWeatherMapService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<WeatherForecastDTO?> GetForecastAsync(string city)
        {
            var apiKey = _configuration["OpenWeatherMap:ApiKey"];
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json)!;

            return new WeatherForecastDTO
            {
                DateWeather = DateTime.Now,
                TemperatureC = "(int)data.main.temp",
                Summary = data.weather[0].main,
                Humidity = "(int)data.main.humidity",
                RainfallMm = "data.rain?.[\"1h\"] ?? 0",
                WindSpeedKmh = "(double)data.wind.speed * 3.6" // m/s to km/h
            };
        }
    }
}
