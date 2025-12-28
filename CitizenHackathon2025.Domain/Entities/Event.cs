namespace CitizenHackathon2025.Domain.Entities
{
    public class Event
    {
    #nullable disable
        public int Id { get; set; }
        public string Name { get; set; }
        public int? PlaceId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime DateEvent { get; set; }
        public int? ExpectedCrowd { get; set; }
        public bool IsOutdoor { get; set; }
        public bool Active { get; set; }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.