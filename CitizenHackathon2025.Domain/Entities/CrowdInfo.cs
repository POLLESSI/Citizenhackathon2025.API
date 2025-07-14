namespace CitizenHackathon2025.Domain.Entities
{
    public class CrowdInfo
    {
    #nullable disable
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string CrowdLevel { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true; 
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.