namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public sealed class SimulateAntennaZoneRequest
    {
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusMeters { get; set; } = 5_000;

        public int DeviceCount { get; set; } = 1_000;
        public int DurationSeconds { get; set; } = 300;
        public int JitterPercent { get; set; } = 10;
        public bool BurstMode { get; set; }

        public int? EventId { get; set; }
        public string Scenario { get; set; } = "RaveParty";
    }
}
