using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Contracts.DTOs;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IWeatherForecastBroadcaster
    {
        Task BroadcastForecastAsync(WeatherForecastDTO dto, CancellationToken ct = default);
        Task BroadcastHeavyRainAlertAsync(RainAlertDTO alert, CancellationToken ct = default);
        Task BroadcastArchivedAsync(int id, CancellationToken ct = default);
    }
}








































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.