namespace CitizenHackathon2025.Domain.Entities
{
    public class CrowdInfo
    {
        public int Id { get; set; }
        public string LocationName { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int CrowdLevel { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true;
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.