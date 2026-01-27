namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public sealed class AntennaSnapshotDTO
    {
        public int AntennaId { get; init; }
        public DateTime WindowStartUtc { get; init; }
        public short WindowSeconds { get; init; }

        public int ActiveConnections { get; init; }
        public byte Confidence { get; init; } // 0..100
        public byte Source { get; init; }     // 1=WallonieEnPoche
    }
}
