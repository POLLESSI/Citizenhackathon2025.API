using CitizenHackathon2025.Shared.Interfaces;

namespace CitizenHackathon2025.Shared.Options
{
    public class DailyArchiverOptions : IDailyArchiverOptions
    { 
        public string TimeZone { get; set; } = "Europe/Brussels"; 
        public int Hour { get; set; } = 2; 
        public int Minute { get; set; } = 0; 
    }
}
