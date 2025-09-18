using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenWeather;
using CitizenHackathon2025.Shared.Json;
using CitizenHackathon2025.Shared.Options;               
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class OpenWeatherService : IOpenWeatherService
    {
        private readonly HttpClient _http;
        private readonly ILogger<OpenWeatherService> _logger;
        private readonly OpenWeatherOptions _opt;

        private const string GeoPath = "/geo/1.0/direct";
        private const string CurrentWeatherPath = "/data/2.5/weather";
        private const string ForecastPath = "/data/2.5/forecast";

        public OpenWeatherService(
            HttpClient http,
            ILogger<OpenWeatherService> logger,
            IOptions<OpenWeatherOptions> options)
        {
            _http = http;
            _logger = logger;
            _opt = options.Value ?? throw new ArgumentNullException(nameof(options));

            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(_opt.BaseUrl ?? "https://api.openweathermap.org");
        }

        public async Task<(double lat, double lon)?> GetCoordinatesAsync(string city)
        {
            try
            {
                var url = $"{GeoPath}?q={Uri.EscapeDataString(city)}&limit=1&appid={_opt.ApiKey}";
                var response = await _http.GetFromJsonAsync<List<GeoLocationDTO>>(url, JsonDefaults.Options);
                var loc = response?.FirstOrDefault();
                return loc is null ? null : (loc.Lat, loc.Lon);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error retrieving coordinates for {City}", city);
                return null;
            }
        }

        public async Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
        {
            try
            {
                var url = $"{CurrentWeatherPath}?q={Uri.EscapeDataString(city)}&appid={_opt.ApiKey}&units=metric&lang=fr";
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Current weather call failed for {City} : {Status}", city, resp.StatusCode);
                    return null;
                }
                var json = await resp.Content.ReadAsStringAsync(ct);
                return ParseWeatherDto(JsonDocument.Parse(json).RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetCurrentWeatherAsync");
                return null;
            }
        }

        public async Task<WeatherForecastDTO?> GetForecastAsync(string city, CancellationToken ct = default)
        {
            try
            {
                var url = $"{ForecastPath}?q={Uri.EscapeDataString(city)}&appid={_opt.ApiKey}&units=metric&lang=fr";
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Forecast call failed for {City} : {Status}", city, resp.StatusCode);
                    return null;
                }
                var json = await resp.Content.ReadAsStringAsync(ct);
                var first = JsonDocument.Parse(json).RootElement.GetProperty("list")[0];
                return ParseWeatherDto(first, DateTime.UtcNow.AddHours(3));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetForecastAsync");
                return null;
            }
        }

        public async Task<WeatherForecastDTO> GetForecastAsync(decimal latitude, decimal longitude, CancellationToken ct = default)
        {
            var lat = latitude.ToString(CultureInfo.InvariantCulture);
            var lon = longitude.ToString(CultureInfo.InvariantCulture);
            var url = $"{CurrentWeatherPath}?lat={lat}&lon={lon}&units=metric&appid={_opt.ApiKey}";
            _logger.LogDebug("OpenWeather call: {Url}", url);

            var ow = await _http.GetFromJsonAsync<OpenWeatherResponse>(url, JsonDefaults.Options, ct)
                     ?? throw new InvalidOperationException("Empty OpenWeather response");

            return new WeatherForecastDTO
            {
                DateWeather = DateTimeOffset.FromUnixTimeSeconds(ow.dt).UtcDateTime,
                TemperatureC = (int)Math.Round(ow.main.temp),
                Summary = ow.weather.FirstOrDefault()?.description,
                Humidity = ow.main.humidity,
                WindSpeedKmh = ow.wind.speed * 3.6,
                RainfallMm = 0d
            };
        }

        public async Task<WeatherForecastDTO?> GetWeatherAsync(double lat, double lon, CancellationToken ct = default)
        {
            try
            {
                var url = $"{CurrentWeatherPath}?lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lon.ToString(CultureInfo.InvariantCulture)}&appid={_opt.ApiKey}&units=metric&lang=fr";
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Weather call for coordinates failed {Lat}, {Lon}", lat, lon);
                    return null;
                }
                var json = await resp.Content.ReadAsStringAsync(ct);
                return ParseWeatherDto(JsonDocument.Parse(json).RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetWeatherAsync");
                return null;
            }
        }

        public async Task<string> GetWeatherSummaryAsync(string location, CancellationToken ct = default)
        {
            var weather = await GetCurrentWeatherAsync(location, ct);
            if (weather is null) return $"Unable to retrieve weather for {location}.";
            return $"Il fait {weather.TemperatureC}°C avec {weather.Summary}, vent {weather.WindSpeedKmh:F1} km/h et humidité {weather.Humidity}%.";
        }

        // ---------- "CT-free" wrappers if your interface still requires them ----------
        Task<WeatherForecastDTO?> IOpenWeatherService.GetCurrentWeatherAsync(string city)
            => GetCurrentWeatherAsync(city, default);

        Task<WeatherForecastDTO?> IOpenWeatherService.GetForecastAsync(string city)
            => GetForecastAsync(city, default);

        Task<WeatherForecastDTO?> IOpenWeatherService.GetWeatherAsync(double lat, double lon)
            => GetWeatherAsync(lat, lon, default);

        Task<string> IOpenWeatherService.GetWeatherSummaryAsync(string location)
            => GetWeatherSummaryAsync(location, default);
        // ------------------------------------------------------------------------

        private static WeatherForecastDTO ParseWeatherDto(JsonElement root, DateTime? dateOverride = null)
        {
            var date = dateOverride ?? DateTime.UtcNow;
            var main = root.GetProperty("main");
            var wind = root.GetProperty("wind");
            var weatherArray = root.GetProperty("weather");
            var description = weatherArray[0].GetProperty("description").GetString() ?? "N/A";

            double rainfall = 0;
            if (root.TryGetProperty("rain", out var rain) && rain.TryGetProperty("1h", out var rain1h))
                rain1h.TryGetDouble(out rainfall);

            return new WeatherForecastDTO
            {
                DateWeather = date,
                TemperatureC = (int)main.GetProperty("temp").GetDouble(),
                Summary = description,
                Humidity = main.GetProperty("humidity").GetInt32(),
                RainfallMm = rainfall,
                WindSpeedKmh = wind.GetProperty("speed").GetDouble() * 3.6
            };
        }

        // DTO for /geo/1.0/direct
        public class GeoLocationDTO { public double Lat { get; set; } public double Lon { get; set; } }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.