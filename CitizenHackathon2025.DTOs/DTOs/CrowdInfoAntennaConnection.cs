namespace CitizenHackathon2025.DTOs.DTOs
{
    public class CrowdInfoAntennaConnection
    {
        public Guid Id { get; set; }
        public int AntennaId { get; set; }
        public byte DeviceHash { get; set; }
        public byte IpHash { get; set; }
        public Byte MacHash { get; set; }
        public int Source { get; set; }
        public int SignalStrength { get; set; }
        public string Band { get; set; } = "";
        public DateTime FirstSeenUtc { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public string AdditionalJson { get; set; } = "";
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.