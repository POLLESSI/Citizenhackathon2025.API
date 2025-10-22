using CitizenHackathon2025.Shared.Interfaces;

namespace CitizenHackathon2025.Shared.Options
{
    public sealed class CrowdInfoArchiverOptions : DailyArchiverOptions, IDailyArchiverOptions
    {
        public string TimeZone { get; set; } = "Europe/Brussels"; // IANA
        public int Hour { get; set; } = 2;   // 02:00
        public int Minute { get; set; } = 0;
    }
}
