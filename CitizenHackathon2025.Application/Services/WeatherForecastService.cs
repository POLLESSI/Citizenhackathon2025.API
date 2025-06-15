using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Application.Extensions;
using Microsoft.Extensions.Logging;
using Citizenhackathon2025.Shared.DTOs;

namespace Citizenhackathon2025.Application.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
    #nullable disable
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly Random _rng = new();

        public WeatherForecastService(IWeatherForecastRepository weatherRepository)
        {
            _weatherRepository = weatherRepository;
        }

        public Task AddAsync(WeatherForecastDTO weatherForecast)
        {
            throw new NotImplementedException();
        }
        Task<WeatherForecastDTO> IWeatherForecastService.GenerateNewForecastAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<WeatherForecastDTO> GenerateNewForecastAsync()
        {
            var forecast = new WeatherForecastDTO
            {
                DateWeather = DateTime.Now,
                TemperatureC = _rng.Next(-10, 35),
                Summary = "Généré",
                RainfallMm = Math.Round(_rng.NextDouble() * 20, 1),
                Humidity = _rng.Next(30, 100),
                WindSpeedKmh = Math.Round(_rng.NextDouble() * 80, 1)
            };
            var entity = forecast.MapToWeatherForecast();

            var saved = await _weatherRepository.SaveWeatherForecastAsync(entity);
            return saved.MapToWeatherForecastDTO();
        }

        public Task<List<WeatherForecastDTO>> GetAllAsync(Domain.Entities.WeatherForecast forecast)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync()
        {
            var weatherForecasts = await _weatherRepository.GetLatestWeatherForecastAsync();
            return null;
        }

        public async Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO weatherForecast)
        {
            var entity = weatherForecast.MapToWeatherForecast();
            var saved = await _weatherRepository.SaveWeatherForecastAsync(entity);

            return saved.MapToWeatherForecastDTO();
        }

        public Task SendWeatherToAllClientsAsync()
        {
            throw new NotImplementedException();
        }

        async Task<List<WeatherForecastDTO>> IWeatherForecastService.GetHistoryAsync(int limit = 128)
        {
            var entities = await _weatherRepository.GetHistoryAsync();
            return entities.Select(e => e.MapToWeatherForecastDTO()).ToList();
        }

        Task<IEnumerable<WeatherForecastDTO>> IWeatherForecastService.GetLatestWeatherForecastAsync()
        {
            throw new NotImplementedException();
        }
    }
}
