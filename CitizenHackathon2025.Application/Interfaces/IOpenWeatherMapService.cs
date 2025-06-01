using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Shared.DTOs;
using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IOpenWeatherMapService
    {
        Task<WeatherForecastDTO?> GetForecastAsync(string city);
    }
}
