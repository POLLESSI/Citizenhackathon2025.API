using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Citizenhackathon2025.Infrastructure.Services
{
    /// <summary>
    /// Weather data retrieval service via the OpenWeather API.
    /// </summary>
    public class OpenWeatherService : IOpenWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenWeatherService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        private const string GeoPath = "/geo/1.0/direct";
        private const string CurrentWeatherPath = "/data/2.5/weather";
        private const string ForecastPath = "/data/2.5/forecast";

        public OpenWeatherService( HttpClient httpClient, ILogger<OpenWeatherService> logger, IOptions<OpenWeatherOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = options.Value.ApiKey;
            _baseUrl = options.Value.BaseUrl;
        }

        /// <inheritdoc />
        public async Task<(double lat, double lon)?> GetCoordinatesAsync(string city)
        {
            try
            {
                var url = $"{_baseUrl}{GeoPath}?q={city}&limit=1&appid={_apiKey}";
                var response = await _httpClient.GetFromJsonAsync<List<GeoLocationDTO>>(url);
                var location = response?.FirstOrDefault();

                return location is not null ? (location.Lat, location.Lon) : null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error retrieving coordinates for {City}", city);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city)
        {
            try
            {
                var url = $"{_baseUrl}{CurrentWeatherPath}?q={city}&appid={_apiKey}&units=metric&lang=fr";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Current weather call failed for {City} : {Status}", city, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return ParseWeatherDto(JsonDocument.Parse(json).RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception dans GetCurrentWeatherAsync");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<WeatherForecastDTO?> GetForecastAsync(string city)
        {
            try
            {
                var url = $"{_baseUrl}{ForecastPath}?q={city}&appid={_apiKey}&units=metric&lang=fr";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Forecast call failed for {City} : {Status}", city, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var forecast = JsonDocument.Parse(json).RootElement.GetProperty("list")[0];

                return ParseWeatherDto(forecast, DateTime.UtcNow.AddHours(3));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetForecastAsync");
                return null;
            }
        }

        public async Task<string> GetWeatherSummaryAsync(string location)
        {
            var weather = await GetCurrentWeatherAsync(location);
            if (weather == null)
                return $"Unable to retrieve weather for {location}.";

            return $"He does {weather.TemperatureC}°C with {weather.Summary}, " +
                   $"wind to {weather.WindSpeedKmh:F1} km/h and humidity of {weather.Humidity}%.";
        }

        /// <inheritdoc />
        public async Task<WeatherForecastDTO?> GetWeatherAsync(double lat, double lon)
        {
            try
            {
                var url = $"{_baseUrl}{CurrentWeatherPath}?lat={lat}&lon={lon}&appid={_apiKey}&units=metric&lang=fr";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Weather call for coordinates failed {Lat}, {Lon}", lat, lon);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return ParseWeatherDto(JsonDocument.Parse(json).RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetWeatherAsync");
                return null;
            }
        }
        /// <summary>
        /// Converts a weather JSON element to a DTO.
        /// </summary>
        private static WeatherForecastDTO ParseWeatherDto(JsonElement root, DateTime? dateOverride = null)
        {
            var date = dateOverride ?? DateTime.UtcNow;
            var main = root.GetProperty("main");
            var wind = root.GetProperty("wind");
            var weatherArray = root.GetProperty("weather");
            var description = weatherArray[0].GetProperty("description").GetString() ?? "N/A";

            double rainfall = 0;
            if (root.TryGetProperty("rain", out var rain))
            {
                rain.TryGetProperty("1h", out var rain1h);
                rain1h.TryGetDouble(out rainfall);
            }

            return new WeatherForecastDTO
            {
                DateWeather = date,
                TemperatureC = (int)main.GetProperty("temp").GetDouble(),
                Summary = description,
                Humidity = main.GetProperty("humidity").GetInt32(),
                RainfallMm = rainfall,
                WindSpeedKmh = wind.GetProperty("speed").GetDouble() * 3.6 // m/s → km/h
            };
        }

    }
    // DTO used for geolocation
    public class GeoLocationDTO
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.