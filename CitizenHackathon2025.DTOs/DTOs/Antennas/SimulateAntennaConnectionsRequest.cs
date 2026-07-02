namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public sealed class SimulateAntennaConnectionsRequest
    {
        public int AntennaId { get; set; }
        public int? EventId { get; set; }
        public int DeviceCount { get; set; }
        public int DurationSeconds { get; set; } = 60;
        public int JitterPercent { get; set; } = 10;
        public bool BurstMode { get; set; }

        public string Scenario { get; set; } = "Static";
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.