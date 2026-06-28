using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IWeatherForecastRepository
    {
        Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity, CancellationToken ct = default);

        Task<WeatherForecast?> GetLatestWeatherForecastAsync(CancellationToken ct = default);
        Task<WeatherForecast> GetCurrentWeatherAsync(double? latitude, double? longitude, CancellationToken ct = default);
        Task<List<WeatherForecast>> GetAllAsync(CancellationToken ct = default);
        Task<List<WeatherForecast>> GetByLocationAsync(decimal latitude, decimal longitude, decimal delta = 0.05m, CancellationToken ct = default);
        Task<List<WeatherForecast>> GetByWeatherTypeAsync(WeatherType weatherType, CancellationToken ct = default);
        Task<List<WeatherForecast>> GetByProviderAsync(WeatherProvider provider, CancellationToken ct = default);
        Task<List<WeatherForecast>> GetByIsSevereAsync(bool isSevere, CancellationToken ct = default);
        Task<List<WeatherForecast>> GetHistoryAsync(int limit = 128, CancellationToken ct = default);
        Task<WeatherForecast?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<WeatherForecast> GenerateNewForecastAsync(CancellationToken ct = default);
        Task<int> ArchivePastWeatherForecastsAsync(CancellationToken ct = default);
        Task<(double LastHour, double Last72h)> GetRainAccumulationAsync(decimal latitude, decimal longitude, DateTime asOfUtc,CancellationToken ct = default);
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.