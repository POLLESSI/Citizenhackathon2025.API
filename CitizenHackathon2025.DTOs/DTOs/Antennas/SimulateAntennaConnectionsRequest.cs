namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public sealed class SimulateAntennaConnectionsRequest
    {
        public int AntennaId { get; set; }
        public int? EventId { get; set; }

        public int DeviceCount { get; set; } = 50;

        // Duration during which the devices remain "alive"
        public int DurationSeconds { get; set; } = 60;

        // Random variation to avoid an overly perfect curve
        public int JitterPercent { get; set; } = 15;

        // Useful for simulating a rave / sudden concentration
        public bool BurstMode { get; set; } = false;
    }
}











































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.