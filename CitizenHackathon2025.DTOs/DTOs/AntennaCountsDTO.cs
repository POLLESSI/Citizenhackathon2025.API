namespace CitizenHackathon2025.DTOs.DTOs
{
    public class AntennaCountsDTO 
    { 
        public int ActiveConnections { get; set; } 
        public int UniqueDevices { get; set; }
        public DateTime WindowStartUtc { get; set; }
        public DateTime WindowEndUtc { get; set; }
        public int WindowMinutes { get; set; }
    }
}
