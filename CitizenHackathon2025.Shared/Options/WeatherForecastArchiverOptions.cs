using CitizenHackathon2025.Shared.Interfaces;

namespace CitizenHackathon2025.Shared.Options
{
    public sealed class WeatherForecastArchiverOptions : DailyArchiverOptions, IDailyArchiverOptions
    {
        public string TimeZone { get; set; } = "Europe/Brussels";
        public int Hour { get; set; } = 2;
        public int Minute { get; set; } = 0;
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.