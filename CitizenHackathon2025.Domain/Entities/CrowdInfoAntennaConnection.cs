namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class CrowdInfoAntennaConnection
    {
        public long Id { get; set; }                 // BIGINT IDENTITY
        public int AntennaId { get; set; }

        public byte[] DeviceHash { get; set; } = Array.Empty<byte>();  // BINARY(32)
        public byte[]? IpHash { get; set; }                             // BINARY(32) NULL
        public byte[]? MacHash { get; set; }                            // BINARY(32) NULL

        public byte Source { get; set; }            // TINYINT
        public short? SignalStrength { get; set; }  // SMALLINT NULL
        public string? Band { get; set; }           // NVARCHAR(16) NULL

        public DateTime FirstSeenUtc { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public bool Active { get; set; }
        public string? AdditionalJson { get; set; }
    }
}
