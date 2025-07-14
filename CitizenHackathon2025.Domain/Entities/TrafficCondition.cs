namespace CitizenHackathon2025.Domain.Entities
{
    public class TrafficCondition
    {
    #nullable disable
        public int Id { get; private set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public DateTime DateCondition { get; set; }
        public string CongestionLevel { get; set; }
        public string IncidentType { get; set; }
        public bool Active { get; private set; } = true;

    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.