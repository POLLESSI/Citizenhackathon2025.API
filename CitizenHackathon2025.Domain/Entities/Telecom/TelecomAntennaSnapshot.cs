namespace CitizenHackathon2025.Domain.Entities.Telecom
{
    public sealed class TelecomAntennaSnapshot
    {
        public string Provider { get; set; } = "";
        public DateTime RetrievedAtUtc { get; set; }
        public List<TelecomAntennaRecord> Antennas { get; set; } = [];
    }
}
