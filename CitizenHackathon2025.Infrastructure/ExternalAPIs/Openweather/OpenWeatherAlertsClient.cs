using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather
{
    public sealed class OpenWeatherAlertsClient : IOpenWeatherAlertsClient
    {
        private readonly HttpClient _http;
        private readonly OpenWeatherOptions _opt;
        private readonly ILogger<OpenWeatherAlertsClient> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public OpenWeatherAlertsClient(
            HttpClient http,
            IOptions<OpenWeatherOptions> opt,
            ILogger<OpenWeatherAlertsClient> logger)
            => (_http, _opt, _logger) = (http, opt.Value, logger);

        public async Task<OneCallResponse> GetOneCallAsync(decimal lat, decimal lon, CancellationToken ct = default)
        {
            var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

            var key = _opt.ApiKey;
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("OpenWeather ApiKey is missing (OpenWeather:ApiKey).");

            var url = QueryHelpers.AddQueryString($"{baseUrl}/data/3.0/onecall", new Dictionary<string, string?>
            {
                ["lat"] = lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["lon"] = lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["units"] = "metric",
                ["lang"] = "fr",
                ["exclude"] = "minutely,hourly,daily",
                ["appid"] = key
            });

            // Log safe: Keyless URL
            _logger.LogInformation("OpenWeather OneCall request url={Url}", MaskAppId(url));

            using var resp = await _http.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OpenWeather OneCall failed status={Status} body={Body}",
                    (int)resp.StatusCode, Trim(body, 300));
            }

            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<OneCallResponse>(JsonOpts, ct)) ?? new OneCallResponse();
        }

        private static string MaskAppId(string url)
            => System.Text.RegularExpressions.Regex.Replace(url, @"appid=[^&]+", "appid=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static string Trim(string s, int max)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max) + "…");
    }

}



























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.