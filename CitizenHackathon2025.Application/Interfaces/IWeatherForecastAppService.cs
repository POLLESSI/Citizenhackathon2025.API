using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IWeatherForecastAppService
    {
        Task<WeatherForecastDTO> CreateAsync(WeatherForecastDTO dto, CancellationToken ct = default);
        Task<WeatherForecastDTO> ManualAsync(WeatherForecastDTO dto, CancellationToken ct = default);

        Task<(int alertsUpserted, WeatherForecastDTO? forecastSaved)> PullAsync(decimal lat, decimal lon, CancellationToken ct = default);

        Task<WeatherForecastDTO> GenerateAsync(CancellationToken ct = default);

        Task<List<WeatherForecastDTO>> GetAllAsync(CancellationToken ct = default);
        Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit, CancellationToken ct = default);
        Task<WeatherForecastDTO?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<IReadOnlyList<WeatherForecastDTO>> GetCurrentAsync(string? city, CancellationToken ct = default);

        Task<int> ArchiveExpiredAsync(CancellationToken ct = default);

        Task<WeatherForecastDTO> UpdateAsync(int id, WeatherForecastDTO dto, CancellationToken ct = default);
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.