using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Json;

namespace Citizenhackathon2025.Infrastructure.ExternalAPIs
{
    public class OpenWeatherMapClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenWeatherMapClient> _logger;

        public OpenWeatherMapClient(HttpClient httpClient, IConfiguration configuration, ILogger<OpenWeatherMapClient> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenWeather:ApiKey"] ?? throw new ArgumentNullException("OpenWeather:ApiKey not configured");
            _logger = logger;
        }

        public async Task<WeatherInfoDTO?> GetWeatherAsync(string city)
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric&lang=fr";
            return await GetWeatherFromApiAsync(url);
        }

        public async Task<WeatherInfoDTO?> GetWeatherAsync(double latitude, double longitude)
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang=fr";
            return await GetWeatherFromApiAsync(url);
        }

        private async Task<WeatherInfoDTO?> GetWeatherFromApiAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeatherMap returned {Code} for URL '{Url}'", response.StatusCode, url);
                    return null;
                }

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());

                long? sunriseUnix = json["sys"]?["sunrise"]?.Value<long>();
                long? sunsetUnix = json["sys"]?["sunset"]?.Value<long>();

                var weatherInfo = new WeatherInfoDTO
                {
                    Location = json["name"]?.ToString() ?? "Unknown",
                    WeatherDescription = json["weather"]?[0]?["description"]?.ToString() ?? "Not specified",
                    TemperatureCelsius = json["main"]?["temp"]?.Value<double>() ?? 0,
                    FeelsLikeCelsius = json["main"]?["feels_like"]?.Value<double>() ?? 0,
                    Sunrise = sunriseUnix.HasValue ? DateTimeOffset.FromUnixTimeSeconds(sunriseUnix.Value).DateTime.ToLocalTime() : null,
                    Sunset = sunsetUnix.HasValue ? DateTimeOffset.FromUnixTimeSeconds(sunsetUnix.Value).DateTime.ToLocalTime() : null
                };

                return weatherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenWeatherMap with URL : {Url}", url);
                return null;
            }
        }
    }
}
