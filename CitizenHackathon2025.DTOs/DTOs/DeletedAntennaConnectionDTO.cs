namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class DeletedAntennaConnectionDTO
    {
        public long DeletedId { get; set; }
        public long OriginalId { get; set; }
        public int AntennaId { get; set; }
        public int? EventId { get; set; }

        public byte[] DeviceHash { get; set; } = Array.Empty<byte>();
        public byte[]? IpHash { get; set; }
        public byte[]? MacHash { get; set; }

        public byte Source { get; set; }
        public short? SignalStrength { get; set; }
        public short? Rssi { get; set; }
        public string? Band { get; set; }

        public DateTime FirstSeenUtc { get; set; }
        public DateTime LastSeenUtc { get; set; }

        public DateTime DeletedUtc { get; set; }
        public byte DeletedReason { get; set; }
        public string? AdditionalJson { get; set; }
    }
}















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.