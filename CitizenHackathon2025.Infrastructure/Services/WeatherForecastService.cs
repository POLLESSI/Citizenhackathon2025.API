using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class WeatherForecastService : IWeatherForecastService
    {
        private readonly IWeatherForecastAppService _app;

        public WeatherForecastService(IWeatherForecastAppService app)
            => _app = app;

        public Task AddAsync(WeatherForecastDTO dto, CancellationToken ct = default)
            => _app.CreateAsync(dto, ct);

        public Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO dto, CancellationToken ct = default)
            => _app.CreateAsync(dto, ct);

        public Task<WeatherForecastDTO> GenerateNewForecastAsync(CancellationToken ct = default)
            => _app.GenerateAsync(ct);

        public Task<List<WeatherForecastDTO>> GetAllAsync(CancellationToken ct = default)
            => _app.GetAllAsync(ct);
        public Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit = 128, CancellationToken ct = default)
            => _app.GetHistoryAsync(limit, ct);

        public async Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync(CancellationToken ct = default)
            => await _app.GetCurrentAsync(city: null, ct);

        public Task SendWeatherToAllClientsAsync(CancellationToken ct = default)
            => Task.CompletedTask; // or call _app.GetCurrentAsync + broadcaster if you want

        public Task<int> ArchivePastWeatherForecastsAsync(CancellationToken ct = default)
            => _app.ArchiveExpiredAsync(ct);

        // If you keep these legacy signatures :
        public Task<WeatherForecastDTO> GetForecastAsync(string destination, CancellationToken ct = default)
            => _app.GenerateAsync(ct); // or something else, depending on your needs
        public Task<List<WeatherForecastDTO>> GetAllAsync(WeatherForecast forecast, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
        public Task<WeatherForecastDTO?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<RainAlertDTO?> CheckRainfallAlertAsync(WeatherForecast wf, CancellationToken ct = default)
        {
            // TODO: implement business logic
            return Task.FromResult<RainAlertDTO?>(null);
        }
    }
}


















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.