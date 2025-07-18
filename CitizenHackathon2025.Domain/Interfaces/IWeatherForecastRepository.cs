﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.WeatherForecast;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IWeatherForecastRepository
    {
        Task AddAsync(WeatherForecast weatherForecast);
        Task<WeatherForecast?> GetLatestWeatherForecastAsync();
        Task<WeatherForecast> SaveWeatherForecastAsync(WeatherForecast forecast);
        Task<WeatherForecast> GenerateNewForecastAsync();
        Task<List<WeatherForecast>> GetHistoryAsync(int limit = 128);
        Task<WeatherForecast?> GetByIdAsync(int id);
        Task<List<WeatherForecast>> GetAllAsync();
        //WeatetherForecast? UpdateWeatherForecast(WeatherForecast weatherForecast);
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.