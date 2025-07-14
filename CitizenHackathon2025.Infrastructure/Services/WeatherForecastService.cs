using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Hubs.Hubs;
using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
    #nullable disable
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly IHubContext<WeatherHub> _hubContext;
        private readonly Random _rng = new();

        public WeatherForecastService(IWeatherForecastRepository weatherRepository, IHubContext<WeatherHub> hubContext)
        {
            _weatherRepository = weatherRepository;
            _hubContext = hubContext;
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
        public async Task<List<WeatherForecastDTO>> GetAllAsync()
        {
            var entities = await _weatherRepository.GetAllAsync();
            return entities.Select(e => new WeatherForecastDTO
            {
                Id = e.Id,
                DateWeather = e.DateWeather,
                Summary = e.Summary,
                TemperatureC = e.TemperatureC,
                Humidity = e.Humidity,
                RainfallMm = e.RainfallMm,
                WindSpeedKmh = e.WindSpeedKmh
            }).ToList();
        }
        public Task<List<WeatherForecastDTO>> GetAllAsync(WeatherForecast forecast)
        {
            throw new NotImplementedException(); 
        }
        public async Task<WeatherForecastDTO?> GetByIdAsync(int id)
        {
            var entity = await _weatherRepository.GetByIdAsync(id);
            if (entity == null)
                return null;

            var weatherDto = entity.MapToWeatherForecastDTO();
            //var trafficDto = entity.MapToTrafficConditionDTO(); // to implement
            //var crowdDto = entity.MapToCrowdInfoDTO();          // to implement
            //var suggestionDto = entity.MapToSuggestionDTO();    // to implement

            var payload = new
            {
                Weather = weatherDto,
                //Traffic = trafficDto,
                //Crowd = crowdDto,
                //Suggestion = suggestionDto
            };

            await _hubContext.Clients.All.SendAsync("WeatherUpdate", payload);

            return weatherDto;
        }


        public async Task<IEnumerable<WeatherForecastDTO>> GetLatestWeatherForecastAsync()
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

        public Task<WeatherForecastDTO> GetForecastAsync(string destination)
        {
            throw new NotImplementedException();
        }
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.