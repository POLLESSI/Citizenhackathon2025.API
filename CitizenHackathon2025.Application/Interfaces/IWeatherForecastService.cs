﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using static Citizenhackathon2025.Domain.Entities.WeatherForecast;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IWeatherForecastService
    {
    #nullable disable
        Task AddAsync(WeatherForecastDTO weatherForecast);
        Task<IEnumerable<WeatherForecastDTO?>> GetLatestWeatherForecastAsync();
        Task<WeatherForecastDTO> SaveWeatherForecastAsync(WeatherForecastDTO @weatherForecast);
        Task<WeatherForecastDTO> GenerateNewForecastAsync();
        Task<List<WeatherForecastDTO>> GetHistoryAsync(int limit = 128);
        Task<WeatherForecastDTO?> GetByIdAsync(int id);
        Task<List<WeatherForecastDTO>> GetAllAsync();
        Task<List<WeatherForecastDTO>> GetAllAsync(Domain.Entities.WeatherForecast forecast);
        Task SendWeatherToAllClientsAsync();
        //WeatherForecastDTO? UpdateWeatherForecast(WeatherForecastDTO weatherForecast);
        Task<WeatherForecastDTO> GetForecastAsync(string destination);
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.