using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Services;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Interfaces;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Mappers;
using CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Models;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.Openweather.Services
{
    public sealed class OpenWeatherIngestionService : IOpenWeatherIngestionService
    {
        private readonly IWeatherForecastRepository _forecastRepo;
        private readonly IWeatherAlertRepository _alertRepo;
        private readonly IWeatherHubService _weatherHub;
        private readonly IOpenWeatherAlertsClient _oneCall;
        private readonly IOpenWeatherCurrentClient _current;
        private readonly ILogger<OpenWeatherIngestionService> _log;

        public OpenWeatherIngestionService(
            IWeatherForecastRepository forecastRepo,
            IWeatherAlertRepository alertRepo,
            IWeatherHubService weatherHub,
            IOpenWeatherAlertsClient oneCall,
            IOpenWeatherCurrentClient current,
            ILogger<OpenWeatherIngestionService> log)
        {
            _forecastRepo = forecastRepo;
            _alertRepo = alertRepo;
            _weatherHub = weatherHub;
            _oneCall = oneCall;
            _current = current;
            _log = log;
        }

        public async Task<(int AlertsUpserted, WeatherForecastDTO? ForecastSaved)> PullAndStoreAsync(
            decimal lat, decimal lon, CancellationToken ct = default)
        {
            try
            {
                var nowUtc = DateTime.UtcNow;

                // ===== 1) OneCall 3.0 =====
                var resp = await _oneCall.GetOneCallAsync(lat, lon, ct);

                // Alerts
                var alerts = resp.Alerts ?? new List<OneCallAlert>();
                var upserted = 0;

                foreach (var a in alerts)
                {
                    ct.ThrowIfCancellationRequested();
                    var entity = OpenWeatherMappers.MapAlert(a, lat, lon, nowUtc);
                    var saved = await _alertRepo.UpsertAsync(entity, ct);
                    if (saved is not null) upserted++;
                }

                // Forecast current
                if (resp.Current is null)
                {
                    _log.LogWarning("OpenWeather OneCall current=null lat={Lat} lon={Lon}", lat, lon);
                    return (upserted, null);
                }

                var wf = OpenWeatherMappers.MapCurrentToForecast(resp, lat, lon);
                var savedWf = await _forecastRepo.SaveOrUpdateAsync(wf);
                var dto = savedWf.MapToWeatherForecastDTO();
                await _weatherHub.BroadcastWeatherAsync(dto, ct);

                return (upserted, dto);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // ===== 2) Fallback 2.5 current =====
                _log.LogWarning(ex, "OneCall not authorized; fallback to /data/2.5/weather");

                var cur = await _current.GetCurrentAsync(lat, lon, ct);
                var wf = OpenWeatherMappers.MapCurrent25ToForecast(cur); // méthode à créer (voir section 2)

                var savedWf = await _forecastRepo.SaveOrUpdateAsync(wf);
                var dto = savedWf.MapToWeatherForecastDTO();
                await _weatherHub.BroadcastWeatherAsync(dto, ct);

                return (0, dto);
            }
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.