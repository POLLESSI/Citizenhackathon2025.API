namespace Citizenhackathon2025.Domain.Entities
{
    public class Event
    {
#nullable disable
        public int Id { get; set; }
        public string Name { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public DateTime DateEvent { get; set; }
        public string ExpectedCrowd { get; set; }
        public string IsOutdoor { get; set; }
        public bool Active { get; set; }
    }
}
