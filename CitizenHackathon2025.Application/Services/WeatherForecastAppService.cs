using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Interfaces.OpenWeather;
using CitizenHackathon2025.Application.WeatherForecasts.Commands;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using MediatR;

namespace CitizenHackathon2025.Application.Services
{
    public sealed class WeatherForecastAppService : IWeatherForecastAppService
    {
        private readonly IWeatherForecastRepository _repo;
        private readonly IWeatherForecastBroadcaster _broadcaster;
        private readonly IOpenWeatherIngestionService _ingestion;
        private readonly IOpenWeatherService _owm;
        private readonly IMediator _mediator;

        public WeatherForecastAppService(
            IWeatherForecastRepository repo,
            IWeatherForecastBroadcaster broadcaster,
            IOpenWeatherIngestionService ingestion,
            IOpenWeatherService owm,
            IMediator mediator)
        {
            _repo = repo;
            _broadcaster = broadcaster;
            _ingestion = ingestion;
            _owm = owm;
            _mediator = mediator;
        }

        // ------------------------------------------------------------------
        // CREATE / MANUAL
        // ------------------------------------------------------------------
        public async Task<WeatherForecastDTO> CreateAsync(
            WeatherForecastDTO dto,
            CancellationToken ct = default)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            var saved = await _repo.SaveOrUpdateAsync(
                dto.MapToWeatherForecast(),
                ct);

            var result = saved.MapToWeatherForecastDTO();

            await _broadcaster.BroadcastForecastAsync(result, ct);

            return result;
        }

        public Task<WeatherForecastDTO> ManualAsync(
            WeatherForecastDTO dto,
            CancellationToken ct = default)
        {
            // WHY: Same logic as Create → no duplication
            return CreateAsync(dto, ct);
        }

        // ------------------------------------------------------------------
        // GENERATE
        // ------------------------------------------------------------------
        public async Task<WeatherForecastDTO> GenerateAsync(
            CancellationToken ct = default)
        {
            var saved = await _repo.GenerateNewForecastAsync(ct);
            var dto = saved.MapToWeatherForecastDTO();

            await _broadcaster.BroadcastForecastAsync(dto, ct);

            // Isolated Business Rule (CQRS)
            var alert = await _mediator.Send(
                new CheckRainfallAlertCommand(saved),
                ct);

            if (alert is not null)
                await _broadcaster.BroadcastHeavyRainAlertAsync(alert, ct);

            return dto;
        }

        // ------------------------------------------------------------------
        // READ
        // ------------------------------------------------------------------
        public async Task<List<WeatherForecastDTO>> GetAllAsync(
            CancellationToken ct = default)
        {
            var entities = await _repo.GetAllAsync(ct);
            return entities
                .Select(e => e.MapToWeatherForecastDTO())
                .ToList();
        }

        public async Task<List<WeatherForecastDTO>> GetHistoryAsync(
            int limit,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 500);

            var entities = await _repo.GetHistoryAsync(limit, ct);
            return entities
                .Select(e => e.MapToWeatherForecastDTO())
                .ToList();
        }

        public async Task<WeatherForecastDTO?> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            var entity = await _repo.GetByIdAsync(id, ct);
            return entity?.MapToWeatherForecastDTO();
        }

        public async Task<IReadOnlyList<WeatherForecastDTO>> GetCurrentAsync(
            string? city,
            CancellationToken ct = default)
        {
            // 1) via provider if city supplied
            if (!string.IsNullOrWhiteSpace(city))
            {
                var coords = await _owm.GetCoordinatesAsync(city, ct);
                if (coords is null)
                    return Array.Empty<WeatherForecastDTO>();

                var (lat, lon) = coords.Value;

                var (_, forecastSaved) =
                    await _ingestion.PullAndStoreAsync(
                        Convert.ToDecimal(lat),
                        Convert.ToDecimal(lon),
                        ct);

                return forecastSaved is null
                    ? Array.Empty<WeatherForecastDTO>()
                    : new[] { forecastSaved };
            }

            // 2) otherwise last in base
            var last = await _repo.GetLatestWeatherForecastAsync(ct);

            return last is null
                ? Array.Empty<WeatherForecastDTO>()
                : new[] { last.MapToWeatherForecastDTO() };
        }

        // ------------------------------------------------------------------
        // ARCHIVE
        // ------------------------------------------------------------------
        public async Task<int> ArchiveExpiredAsync(CancellationToken ct = default)
        {
            var count = await _repo.ArchivePastWeatherForecastsAsync(ct);

            // OPTION: if you want to notify customers that a refresh is helpful
            // await _broadcaster.BroadcastArchivedAsync(/*id? ou count?*/, ct);

            return count;
        }

        // ------------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------------
        public async Task<WeatherForecastDTO> UpdateAsync(
            int id,
            WeatherForecastDTO dto,
            CancellationToken ct = default)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (id <= 0 || id != dto.Id)
                throw new ArgumentException("Id mismatch.");

            var saved = await _repo.SaveOrUpdateAsync(
                dto.MapToWeatherForecast(),
                ct);

            var result = saved.MapToWeatherForecastDTO();

            await _broadcaster.BroadcastForecastAsync(result, ct);

            return result;
        }

        public async Task<(int alertsUpserted, WeatherForecastDTO? forecastSaved)> PullAsync(decimal lat, decimal lon, CancellationToken ct = default)
        {
            // ✅ method must return a tuple per interface
            var (alertsUpserted, forecastSaved) = await _ingestion.PullAndStoreAsync(lat, lon, ct);
            return (alertsUpserted, forecastSaved);
        }
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.