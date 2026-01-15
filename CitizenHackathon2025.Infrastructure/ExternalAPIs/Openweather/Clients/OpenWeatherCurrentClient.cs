using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenWeather;
using CitizenHackathon2025.Shared.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Clients
{
    public sealed class OpenWeatherCurrentClient : IOpenWeatherCurrentClient
    {
        private readonly HttpClient _http;
        private readonly OpenWeatherOptions _opt;

        public OpenWeatherCurrentClient(HttpClient http, IOptions<OpenWeatherOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        async Task<OpenWeatherResponse> IOpenWeatherCurrentClient.GetCurrentAsync(decimal lat, decimal lon, CancellationToken ct)
        {
            var baseUrl = (_opt.BaseUrl ?? "https://api.openweathermap.org").TrimEnd('/');

            var url = QueryHelpers.AddQueryString($"{baseUrl}/data/2.5/weather", new Dictionary<string, string?>
            {
                ["lat"] = lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["lon"] = lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["units"] = "metric",
                ["lang"] = "fr",
                ["appid"] = _opt.ApiKey
            });

            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<OpenWeatherResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web), ct))
                   ?? new OpenWeatherResponse();
        }
    }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.