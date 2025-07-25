using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.ValueObjects;

namespace CitizenHackathon2025.Domain.Services
{
    public class WeatherForecastDomainService
    {
    #nullable disable
        public bool IsWeatherSuitableForOutdoorActivity(WeatherForecast forecast)
        {
            return forecast.TemperatureC > 10 &&
                   forecast.TemperatureC < 35 &&
                   !forecast.Summary.ToLowerInvariant().Contains("rain");
        }

        public double CalculateComfortIndex(WeatherForecast forecast)
        {
            // Simple formula: higher is better
            var humidityFactor = forecast.Summary.ToLowerInvariant().Contains("humid") ? -5 : 0;
            return (35 - Math.Abs(forecast.TemperatureC - 22)) + humidityFactor;
        }

        public bool IsForecastValid(WeatherForecast forecast)
        {
            return forecast.DateWeather >= DateTime.Today &&
                   forecast.TemperatureC > -60 && forecast.TemperatureC < 60 &&
                   !string.IsNullOrWhiteSpace(forecast.Summary);
        }
    }
}




























































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.