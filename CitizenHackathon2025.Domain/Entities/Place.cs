namespace Citizenhackathon2025.Domain.Entities
{
    public class Place
    {
    #nullable disable
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Indoor { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Capacity { get; set; }
        public string Tag { get; set; }
        public bool Active { get; private set; } = true;
    }
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.