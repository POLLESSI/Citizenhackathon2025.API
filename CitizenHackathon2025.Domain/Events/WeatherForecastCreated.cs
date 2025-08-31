using CitizenHackathon2025.Domain.Entities;
using MediatR;

namespace CitizenHackathon2025.Domain.Events
{
    /// <summary>
    /// Event triggered when a weather forecast is created.
    /// </summary>
    public class WeatherForecastCreated : INotification
    {
        public WeatherForecast Forecast { get; }

        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public WeatherForecastCreated(WeatherForecast forecast)
        {
            Forecast = forecast ?? throw new ArgumentNullException(nameof(forecast));
        }
    }
}























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.