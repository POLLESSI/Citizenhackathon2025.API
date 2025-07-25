using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.SignalR
{
    public interface IRealTimeNotifier
    {
        Task NotifyCrowdInfoUpdate(CrowdInfoUIDTO dto);
        Task NotifyWeatherForecast(WeatherForecastDTO dto);
        Task NotifyTrafficCondition(TrafficConditionDTO dto);
    }
}
