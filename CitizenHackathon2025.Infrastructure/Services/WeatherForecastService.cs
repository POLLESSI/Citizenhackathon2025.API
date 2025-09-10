using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Repositories.Providers.Hubs;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly IWeatherForecastRepository _repo;
        private readonly IHubContext<WeatherHub> _hub;

        public WeatherForecastService(IWeatherForecastRepository repo, IHubContext<WeatherHub> hub)
        {
            _repo = repo;
            _hub = hub;
        }

        public Task AddAsync(WeatherForecastDTO dto, CancellationToken ct = default)
            => SaveWeatherForecastAsync(dto, ct);

        public async Task<WeatherForecastDTO> GenerateNewForecastAsync(CancellationToken ct = default)
            => (await _repo.GenerateNewForecastAsync()).MapToWeatherForecastDTO();

        public async Task<List<WeatherForecastDTO>> GetAllAsync(CancellationToken ct = default)
            => (await _repo.GetAllAsync()).Select(x => x.MapToWeatherForecastDTO()).ToList();

        public Task<List<WeatherForecastDTO>> GetAllAsync(WeatherForecast _, CancellationToken ct = default)
            => GetAllAsync(ct);

        public async Task<WeatherForecastDTO?> GetByIdAsync(int id, CancellationToken ct = default)
            => (await _repo.GetByIdAsync(id))?.MapToWeatherForecastDTO();

        public async Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit = 128, CancellationToken ct = default)
            => (await _repo.GetHistoryAsync(limit)).Select(x => x.MapToWeatherForecastDTO()).ToList();

        public async Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync(CancellationToken ct = default)
        {
            var last = await _repo.GetLatestWeatherForecastAsync();
            return last is null ? Enumerable.Empty<WeatherForecastDTO>() : new[] { last.MapToWeatherForecastDTO() };
        }

        public async Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO dto, CancellationToken ct = default)
        {
            var entity = dto.MapToWeatherForecast();
            var saved = await _repo.SaveOrUpdateAsync(entity);
            var res = saved.MapToWeatherForecastDTO();
            await _hub.Clients.All.SendAsync("ReceiveForecast", res, ct);
            return res;
        }

        public async Task SendWeatherToAllClientsAsync(CancellationToken ct = default)
        {
            var last = await _repo.GetLatestWeatherForecastAsync();
            if (last != null)
                await _hub.Clients.All.SendAsync("ReceiveForecast", last.MapToWeatherForecastDTO(), ct);
        }

        public Task<WeatherForecastDTO> GetForecastAsync(string destination, CancellationToken ct = default)
            => Task.FromResult(new WeatherForecastDTO { Summary = "Not wired here (use OpenWeather controller endpoint)", DateWeather = DateTime.UtcNow });
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.