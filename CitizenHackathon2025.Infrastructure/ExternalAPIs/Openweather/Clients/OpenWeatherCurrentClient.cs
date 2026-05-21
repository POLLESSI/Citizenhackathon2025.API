using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenWeather;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Clients
{
    public sealed class OpenWeatherCurrentClient : IOpenWeatherCurrentClient
    {
        private readonly HttpClient _http;
        private readonly OpenWeatherOptions _opt;
        private readonly ILogger<OpenWeatherCurrentClient> _log;

        private static readonly JsonSerializerOptions JsonOpts =
            new(JsonSerializerDefaults.Web);

        public OpenWeatherCurrentClient(HttpClient http, IOptions<OpenWeatherOptions> opt, ILogger<OpenWeatherCurrentClient> log)
        {
            _http = http;
            _opt = opt.Value;
            _log = log;
        }

        public async Task<OpenWeatherResponse> GetCurrentAsync(decimal lat, decimal lon, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                throw new InvalidOperationException("OpenWeather ApiKey is missing.");

            if (_opt.ApiKey.Contains("NOUVELLE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("OpenWeather ApiKey is still a placeholder.");

            var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

            var url = QueryHelpers.AddQueryString($"{baseUrl}/data/2.5/weather",
                new Dictionary<string, string?>
                {
                    ["lat"] = lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["lon"] = lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["units"] = "metric",
                    ["lang"] = "fr",
                    ["appid"] = _opt.ApiKey
                });

            using var resp = await _http.GetAsync(url, ct);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);

                _log.LogWarning(
                    "OpenWeather unauthorized. Check ApiKey. Body={Body}",
                    Trim(body, 300));

                throw new InvalidOperationException(
                    "OpenWeather returned 401 Unauthorized. Check OpenWeather:ApiKey.");
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);

                _log.LogWarning(
                    "OpenWeather failed. Status={Status} Body={Body}",
                    (int)resp.StatusCode,
                    Trim(body, 300));

                throw new InvalidOperationException(
                    $"OpenWeather failed with status {(int)resp.StatusCode}.");
            }

            return await resp.Content.ReadFromJsonAsync<OpenWeatherResponse>(JsonOpts, ct)
                   ?? new OpenWeatherResponse();
        }

        private static string Trim(string value, int max)
            => string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max] + "…";
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.