namespace Citizenhackathon2025.Domain.Entities
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
        public bool Active { get; set; } = true; // Par défaut, actif
    }
}