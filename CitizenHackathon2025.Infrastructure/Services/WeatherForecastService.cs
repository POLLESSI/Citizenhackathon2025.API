using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Abstractions;
using CitizenHackathon2025.Infrastructure.NoSql.Mongo.Collections;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WeatherForecastService : IWeatherForecastService
    {
        private readonly IWeatherForecastAppService _app;
        private readonly IWeatherSnapshotRepository _weatherSnapshotRepository;
        private readonly ILogger<WeatherForecastService> _logger;

        public WeatherForecastService(IWeatherForecastAppService app, IWeatherSnapshotRepository weatherSnapshotRepository, ILogger<WeatherForecastService> logger)
        {
            _app = app;
            _weatherSnapshotRepository = weatherSnapshotRepository;
            _logger = logger;
        }

        public async Task AddAsync(WeatherForecastDTO dto, CancellationToken ct = default)
        {
            await SaveWeatherForecastAsync(dto, ct);
        }

        public async Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO dto, CancellationToken ct = default)
        {
            var saved = await _app.CreateAsync(dto, ct);

            try
            {
                var snapshot = new WeatherSnapshotDocument
                {
                    WeatherForecastId = saved.Id > int.MaxValue ? null : (int?)saved.Id,
                    PlaceId = null,
                    EventId = null,
                    PlaceName = null,

                    Latitude = (double)saved.Latitude,
                    Longitude = (double)saved.Longitude,

                    TemperatureC = saved.TemperatureC,
                    FeelsLikeC = null,
                    WindSpeedKmh = saved.WindSpeedKmh,
                    RainfallMm = saved.RainfallMm,
                    HumidityPercent = saved.Humidity,

                    WeatherType = saved.WeatherType.ToString(),
                    Severity = null,
                    Provider = saved.Provider.ToString(),

                    Summary = saved.Summary,
                    Description = saved.Description,

                    IsSevere = saved.IsSevere,
                    IsCritical = false,

                    ForecastAtUtc = saved.DateWeather.UtcDateTime,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _weatherSnapshotRepository.InsertAsync(snapshot, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Mongo weather snapshot failed. SQL weather forecast remains saved.");
            }

            return saved;
        }

        public Task<WeatherForecastDTO> GenerateNewForecastAsync(CancellationToken ct = default)
            => _app.GenerateAsync(ct);

        public Task<List<WeatherForecastDTO>> GetAllAsync(CancellationToken ct = default)
            => _app.GetAllAsync(ct);

        public Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit = 128, CancellationToken ct = default)
            => _app.GetHistoryAsync(limit, ct);

        public Task<WeatherForecastDTO?> GetByIdAsync(int id, CancellationToken ct = default)
            => _app.GetByIdAsync(id, ct);

        public async Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync(CancellationToken ct = default)
        {
            var current = await _app.GetCurrentAsync(city: null, ct);
            return current;
        }

        public Task<List<WeatherForecastDTO>> GetByLocationAsync(decimal latitude, decimal longitude, decimal delta = 0.05m, CancellationToken ct = default)
            => _app.GetByLocationAsync(latitude, longitude, delta, ct);

        public Task<List<WeatherForecastDTO>> GetByWeatherTypeAsync(WeatherType weatherType, CancellationToken ct = default)
            => _app.GetByWeatherTypeAsync(weatherType, ct);

        public Task<List<WeatherForecastDTO>> GetByProviderAsync(WeatherProvider provider, CancellationToken ct = default)
            => _app.GetByProviderAsync(provider, ct);

        public Task<List<WeatherForecastDTO>> GetByIsSevereAsync(bool isSevere, CancellationToken ct = default)
            => _app.GetByIsSevereAsync(isSevere, ct);

        public Task SendWeatherToAllClientsAsync(CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<WeatherForecastDTO> GetForecastAsync(string destination, CancellationToken ct = default)
            => throw new NotSupportedException("Generated destination forecasts are disabled. Use OpenWeather pull instead.");

        public Task<int> ArchivePastWeatherForecastsAsync(CancellationToken ct = default)
            => _app.ArchiveExpiredAsync(ct);

        public Task<List<WeatherForecastDTO>> GetAllAsync(WeatherForecast forecast, CancellationToken ct = default)
            => _app.GetAllAsync(ct);

        public Task<RainAlertDTO?> CheckRainfallAlertAsync(WeatherForecast wf, CancellationToken ct = default)
        {
            return Task.FromResult<RainAlertDTO?>(null);
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.