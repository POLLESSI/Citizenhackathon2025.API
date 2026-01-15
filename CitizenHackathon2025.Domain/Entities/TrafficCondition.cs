namespace CitizenHackathon2025.Domain.Entities
{
    public class TrafficCondition
    {
        public int Id { get; private set; }   // <- Id controlled
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime DateCondition { get; set; }
        public string CongestionLevel { get; set; } = "";
        public string IncidentType { get; set; } = "";

        public string Provider { get; set; } = "odwb";
        public string ExternalId { get; set; } = "";
        public byte[] Fingerprint { get; set; } = Array.Empty<byte>();
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        public string? Title { get; set; }
        public string? Road { get; set; }
        public byte? Severity { get; set; }
        public string? GeomWkt { get; set; }

        public bool Active { get; set; } = true;

        // ✅ méthode attendue par ton Mapper
        public TrafficCondition WithId(int id)
        {
            Id = id;
            return this;
        }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.