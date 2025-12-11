using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.ValueObjects;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Hubs.Services;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        # nullable disable
        private readonly IWeatherForecastRepository _repo;
        private readonly IWeatherHubService _weatherHub;

        public WeatherForecastService(IWeatherForecastRepository repo, IWeatherHubService weatherHub)
        {
            _repo = repo;
            _weatherHub = weatherHub;
        }

        public Task AddAsync(WeatherForecastDTO dto, CancellationToken ct = default)
            => SaveWeatherForecastAsync(dto, ct);

        public async Task<WeatherForecastDTO> GenerateNewForecastAsync(CancellationToken ct = default)
            => (await _repo.GenerateNewForecastAsync()).MapToWeatherForecastDTO();

        public async Task<List<WeatherForecastDTO>> GetAllAsync(CancellationToken ct = default)
            => (await _repo.GetAllAsync())
                .Select(x => x.MapToWeatherForecastDTO())
                .ToList();

        public Task<List<WeatherForecastDTO>> GetAllAsync(WeatherForecast _, CancellationToken ct = default)
            => GetAllAsync(ct);

        public async Task<WeatherForecastDTO?> GetByIdAsync(int id, CancellationToken ct = default)
            => (await _repo.GetByIdAsync(id))?.MapToWeatherForecastDTO();

        public async Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit = 128, CancellationToken ct = default)
            => (await _repo.GetHistoryAsync(limit))
                .Select(x => x.MapToWeatherForecastDTO())
                .ToList();

        public async Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync(CancellationToken ct = default)
        {
            var last = await _repo.GetLatestWeatherForecastAsync(ct);
            return last is null
                ? Enumerable.Empty<WeatherForecastDTO>()
                : new[] { last.MapToWeatherForecastDTO() };
        }

        public async Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO dto, CancellationToken ct = default)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var entity = dto.MapToWeatherForecast();
            var saved = await _repo.SaveOrUpdateAsync(entity);
            var res = saved.MapToWeatherForecastDTO();

            // Streaming via the hub service (WeatherForecastHub under the hood)
            await _weatherHub.BroadcastWeatherAsync(res, ct);

            return res;
        }

        public async Task<RainAlertDTO?> CheckRainfallAlertAsync(WeatherForecast wf, CancellationToken ct = default)
        {
            // Pas de pluie => pas d’alerte
            if (wf.RainfallMm <= 0 || wf.Latitude is 0 || wf.Longitude is 0)
                return null;

            (double lastHour, double last72h) = 
                await _repo.GetRainAccumulationAsync(
                    wf.Latitude.Value,
                    wf.Longitude.Value,
                    wf.DateWeather,
                    ct);

            const double HeavyRain1hThresholdMm = 50.0;
            const double HeavyRain72hThresholdMm = 200.0;

            bool hourAlert = lastHour > HeavyRain1hThresholdMm;
            bool longAlert = last72h > HeavyRain72hThresholdMm;

            if (!hourAlert && !longAlert)
                return null;

            var (nearSensitive, sensitiveName) = SensitiveFloodAreas.Check(
                (double)wf.Latitude.Value,
                (double)wf.Longitude.Value);

            var message = BuildRainAlertMessage(hourAlert, longAlert, lastHour, last72h, nearSensitive, sensitiveName);

            return new RainAlertDTO
            {
                Latitude = wf.Latitude,
                Longitude = wf.Longitude,
                Total1h = lastHour,
                Total72h = last72h,
                Message = message,
                IsNearSensitiveArea = nearSensitive,
                SensitiveAreaName = sensitiveName
            };
        }

        private static string BuildRainAlertMessage(
            bool hourAlert,
            bool longAlert,
            double lastHour,
            double last72h,
            bool nearSensitive,
            string? sensitiveName)
        {
            var parts = new List<string>();

            if (hourAlert)
                parts.Add($"Heavy rain : {lastHour:F1} mm over the last hour.");

            if (longAlert)
                parts.Add($"High accumulation : {last72h:F1} mm over the last 72 hours.");

            if (nearSensitive && !string.IsNullOrWhiteSpace(sensitiveName))
                parts.Add($"⚠ Sensitive area : {sensitiveName} — increased risk of flooding or high water.");

            return string.Join(" ", parts);
        }


        public async Task SendWeatherToAllClientsAsync(CancellationToken ct = default)
        {
            var last = await _repo.GetLatestWeatherForecastAsync(ct);
            if (last != null)
            {
                var dto = last.MapToWeatherForecastDTO();
                await _weatherHub.BroadcastWeatherAsync(dto, ct);
            }
        }

        public Task<WeatherForecastDTO> GetForecastAsync(string destination, CancellationToken ct = default)
            => Task.FromResult(new WeatherForecastDTO
            {
                Summary = "Not wired here (use OpenWeather controller endpoint)",
                DateWeather = DateTime.UtcNow
            });

        public Task<int> ArchivePastWeatherForecastsAsync()
            => _repo.ArchivePastWeatherForecastsAsync();
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.