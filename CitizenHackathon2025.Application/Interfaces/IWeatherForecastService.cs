using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IWeatherForecastService
    {
#nullable disable
        Task AddAsync(WeatherForecastDTO weatherForecast, CancellationToken ct = default);
        Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync(CancellationToken ct = default);
        Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO weatherForecast, CancellationToken ct = default);
        Task<WeatherForecastDTO> GenerateNewForecastAsync(CancellationToken ct = default);
        Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit = 128, CancellationToken ct = default);
        Task<WeatherForecastDTO?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<WeatherForecastDTO>> GetAllAsync(CancellationToken ct = default);
        Task<List<WeatherForecastDTO>> GetAllAsync(CitizenHackathon2025.Domain.Entities.WeatherForecast forecast, CancellationToken ct = default);
        Task<RainAlertDTO?> CheckRainfallAlertAsync(WeatherForecast wf, CancellationToken ct = default);
        Task SendWeatherToAllClientsAsync(CancellationToken ct = default);
        Task<WeatherForecastDTO> GetForecastAsync(string destination, CancellationToken ct = default);
        Task<int> ArchivePastWeatherForecastsAsync();
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.