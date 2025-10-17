using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IWeatherForecastRepository
    {
        Task AddAsync(WeatherForecast weatherForecast);
        Task<WeatherForecast?> GetLatestWeatherForecastAsync(CancellationToken ct = default);
        Task<WeatherForecast> SaveWeatherForecastAsync(WeatherForecast forecast);
        Task<WeatherForecast> GenerateNewForecastAsync();
        Task<List<WeatherForecast>> GetHistoryAsync(int limit = 128);
        Task<WeatherForecast?> GetByIdAsync(int id);
        Task<List<WeatherForecast>> GetAllAsync();
        //WeatetherForecast? UpdateWeatherForecast(WeatherForecast weatherForecast);
        Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity);
        Task<WeatherForecast> InsertAsync(WeatherForecast forecast, CancellationToken cancellationToken);
    }
}




































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.