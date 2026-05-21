namespace CitizenHackathon2025.Domain.Entities.Telecom
{
    public sealed class TelecomAntennaRecord
    {
        public string ExternalId { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int? Capacity { get; set; }
        public int? Load { get; set; }
    }
}
