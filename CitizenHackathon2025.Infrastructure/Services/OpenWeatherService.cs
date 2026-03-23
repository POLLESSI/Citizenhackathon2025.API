using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenWeather;
using CitizenHackathon2025.Shared.Json;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

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
            IOptions<OpenWeatherOptions> opt)
        {
            _http = http;
            _logger = logger;
            _opt = opt.Value;
        }

        public async Task<(double lat, double lon)?> GetCoordinatesAsync(string city, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                {
                    _logger.LogError("OpenWeather ApiKey is missing.");
                    return null;
                }

                var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

                var url = QueryHelpers.AddQueryString(
                    $"{baseUrl}{GeoPath}",
                    new Dictionary<string, string?>
                    {
                        ["q"] = city,
                        ["limit"] = "1",
                        ["appid"] = _opt.ApiKey
                    });

                _logger.LogInformation("OpenWeather geocoding request for {City}", city);

                using var resp = await _http.GetAsync(url, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Geocoding failed for {City}. Status={Status}. Body={Body}",
                        city,
                        (int)resp.StatusCode,
                        json);
                    return null;
                }

                var response = JsonSerializer.Deserialize<List<GeoLocationDTO>>(json, JsonDefaults.Options);
                var loc = response?.FirstOrDefault();

                return loc is null ? null : (loc.Lat, loc.Lon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving coordinates for {City}", city);
                return null;
            }
        }

        //public async Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
        //        {
        //            _logger.LogError("OpenWeather ApiKey is missing.");
        //            return null;
        //        }

        //        var url = $"{CurrentWeatherPath}?q={Uri.EscapeDataString(city)}&appid={_opt.ApiKey}&units=metric&lang=fr";
        //        _logger.LogInformation(
        //            "OpenWeather current request for city {City} using {Url}",
        //            city,
        //            url.Replace(_opt.ApiKey, "***"));

        //        using var resp = await _http.GetAsync(url, ct);
        //        var json = await resp.Content.ReadAsStringAsync(ct);

        //        if (!resp.IsSuccessStatusCode)
        //        {
        //            _logger.LogWarning(
        //                "Current weather call failed for {City}. Status={Status}. Body={Body}",
        //                city,
        //                (int)resp.StatusCode,
        //                json);
        //            return null;
        //        }

        //        using var doc = JsonDocument.Parse(json);
        //        var dto = ParseWeatherDto(doc.RootElement);

        //        _logger.LogInformation(
        //            "Parsed current weather successfully for {City}: Temp={Temp}, Summary={Summary}",
        //            city,
        //            dto.TemperatureC,
        //            dto.Summary);

        //        return dto;
        //    }
        //    catch (JsonException ex)
        //    {
        //        _logger.LogError(ex, "JSON parsing error in GetCurrentWeatherAsync for city {City}", city);
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Exception in GetCurrentWeatherAsync for city {City}", city);
        //        return null;
        //    }
        //}

        public async Task<WeatherForecastDTO?> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                throw new InvalidOperationException("OpenWeather ApiKey is missing.");

            var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

            var url = QueryHelpers.AddQueryString(
                $"{baseUrl}{CurrentWeatherPath}",
                new Dictionary<string, string?>
                {
                    ["q"] = city,
                    ["appid"] = _opt.ApiKey,
                    ["units"] = "metric",
                    ["lang"] = "fr"
                });

            _logger.LogInformation(
                "OpenWeather current request for city {City} using {Url}",
                city,
                url.Replace(_opt.ApiKey, "***"));

            using var resp = await _http.GetAsync(url, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            _logger.LogInformation(
                "OpenWeather raw response for {City}. Status={Status}. Body={Body}",
                city,
                (int)resp.StatusCode,
                json);

            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(json);

            var dto = ParseWeatherDto(doc.RootElement);

            _logger.LogInformation(
                "Parsed current weather successfully for {City}: Temp={Temp}, Summary={Summary}",
                city,
                dto.TemperatureC,
                dto.Summary);

            return dto;
        }

        public async Task<WeatherForecastDTO?> GetForecastAsync(string city, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                {
                    _logger.LogError("OpenWeather ApiKey is missing.");
                    return null;
                }

                var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

                var url = QueryHelpers.AddQueryString(
                    $"{baseUrl}{ForecastPath}",
                    new Dictionary<string, string?>
                    {
                        ["q"] = city,
                        ["appid"] = _opt.ApiKey,
                        ["units"] = "metric",
                        ["lang"] = "fr"
                    });

                _logger.LogInformation(
                    "OpenWeather forecast request for city {City} using {Url}",
                    city,
                    url.Replace(_opt.ApiKey, "***"));

                using var resp = await _http.GetAsync(url, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Forecast call failed for {City}. Status={Status}. Body={Body}",
                        city,
                        (int)resp.StatusCode,
                        json);
                    return null;
                }

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("list", out var list) || list.GetArrayLength() == 0)
                {
                    _logger.LogWarning(
                        "Forecast response for {City} does not contain any items. Body={Body}",
                        city,
                        json);
                    return null;
                }

                var first = list[0];
                return ParseWeatherDto(first, DateTime.UtcNow.AddHours(3));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error in GetForecastAsync for city {City}", city);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetForecastAsync for city {City}", city);
                return null;
            }
        }

        public async Task<WeatherForecastDTO> GetForecastAsync(decimal latitude, decimal longitude, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                throw new InvalidOperationException("OpenWeather ApiKey is missing.");

            var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

            var url = QueryHelpers.AddQueryString(
                $"{baseUrl}{CurrentWeatherPath}",
                new Dictionary<string, string?>
                {
                    ["lat"] = latitude.ToString(CultureInfo.InvariantCulture),
                    ["lon"] = longitude.ToString(CultureInfo.InvariantCulture),
                    ["units"] = "metric",
                    ["appid"] = _opt.ApiKey
                });

            _logger.LogDebug("OpenWeather call: {Url}", url.Replace(_opt.ApiKey, "***"));

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
                if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                {
                    _logger.LogError("OpenWeather ApiKey is missing.");
                    return null;
                }

                var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

                var url = QueryHelpers.AddQueryString(
                    $"{baseUrl}{CurrentWeatherPath}",
                    new Dictionary<string, string?>
                    {
                        ["lat"] = lat.ToString(CultureInfo.InvariantCulture),
                        ["lon"] = lon.ToString(CultureInfo.InvariantCulture),
                        ["appid"] = _opt.ApiKey,
                        ["units"] = "metric",
                        ["lang"] = "fr"
                    });

                using var resp = await _http.GetAsync(url, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Weather call for coordinates failed {Lat}, {Lon}. Status={Status}. Body={Body}",
                        lat,
                        lon,
                        (int)resp.StatusCode,
                        json);
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                return ParseWeatherDto(doc.RootElement);
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
            if (weather is null)
                return $"Unable to retrieve weather for {location}.";

            return $"Il fait {weather.TemperatureC}°C avec {weather.Summary}, vent {weather.WindSpeedKmh:F1} km/h et humidité {weather.Humidity}%.";
        }

        Task<WeatherForecastDTO?> IOpenWeatherService.GetCurrentWeatherAsync(string city)
            => GetCurrentWeatherAsync(city, default);

        Task<WeatherForecastDTO?> IOpenWeatherService.GetForecastAsync(string city)
            => GetForecastAsync(city, default);

        Task<WeatherForecastDTO?> IOpenWeatherService.GetWeatherAsync(double lat, double lon)
            => GetWeatherAsync(lat, lon, default);

        Task<string> IOpenWeatherService.GetWeatherSummaryAsync(string location)
            => GetWeatherSummaryAsync(location, default);

        private static WeatherForecastDTO ParseWeatherDto(JsonElement root, DateTime? dateOverride = null)
        {
            var date = dateOverride ?? DateTime.UtcNow;

            if (!root.TryGetProperty("main", out var main))
                throw new InvalidOperationException("OpenWeather response missing 'main' property.");

            if (!root.TryGetProperty("wind", out var wind))
                throw new InvalidOperationException("OpenWeather response missing 'wind' property.");

            if (!root.TryGetProperty("weather", out var weatherArray) ||
                weatherArray.ValueKind != JsonValueKind.Array ||
                weatherArray.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("OpenWeather response missing 'weather[0]' property.");
            }

            var weather0 = weatherArray[0];

            var weatherMain = weather0.TryGetProperty("main", out var mainEl)
                ? mainEl.GetString() ?? string.Empty
                : string.Empty;

            var description = weather0.TryGetProperty("description", out var descEl)
                ? descEl.GetString()
                : null;

            var icon = weather0.TryGetProperty("icon", out var iconEl)
                ? iconEl.GetString()
                : null;

            var temp = main.TryGetProperty("temp", out var tempEl)
                ? tempEl.GetDouble()
                : throw new InvalidOperationException("OpenWeather response missing 'main.temp'.");

            var humidity = main.TryGetProperty("humidity", out var humEl)
                ? humEl.GetInt32()
                : 0;

            var windSpeed = wind.TryGetProperty("speed", out var speedEl)
                ? speedEl.GetDouble()
                : 0d;

            double rainfall = 0d;
            if (root.TryGetProperty("rain", out var rain) &&
                rain.TryGetProperty("1h", out var rain1h))
            {
                rain1h.TryGetDouble(out rainfall);
            }

            return new WeatherForecastDTO
            {
                DateWeather = date,
                TemperatureC = (int)Math.Round(temp),
                Humidity = humidity,
                WindSpeedKmh = windSpeed * 3.6,
                RainfallMm = rainfall,

                Summary = description ?? "N/A",
                WeatherMain = weatherMain,
                Description = description,
                Icon = icon,
                IconUrl = string.IsNullOrWhiteSpace(icon)
                    ? string.Empty
                    : $"https://openweathermap.org/img/wn/{icon}@2x.png"
            };
        }

        public class GeoLocationDTO
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
        }
    }
}


















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.