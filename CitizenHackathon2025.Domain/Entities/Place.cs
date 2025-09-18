namespace CitizenHackathon2025.Domain.Entities
{
    public class Place
    {
    #nullable disable
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Indoor { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Capacity { get; set; }
        public string Tag { get; set; }
        public bool Active { get; private set; } = true;
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.