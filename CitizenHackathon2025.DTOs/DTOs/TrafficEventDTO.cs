namespace CitizenHackathon2025.DTOs.DTOs
{
    /// <summary>
    /// Domain-independent DTO: no reference to Domain enums/entities.
    /// </summary>
    public class TrafficEventDTO
    {
        public int Id { get; set; } = 0;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        /// <summary>UI scale (eg: 0..5). Map from/to your Domain enum on the Application side.</summary>
        public int Level { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.