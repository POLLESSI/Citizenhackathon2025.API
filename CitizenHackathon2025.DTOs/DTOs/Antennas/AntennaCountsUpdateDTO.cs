namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public sealed class AntennaCountsUpdateDTO
    {
        public int AntennaId { get; init; }
        public DateTime WindowStartUtc { get; init; }
        public short WindowSeconds { get; init; }

        public int ActiveConnections { get; init; }
        public byte Confidence { get; init; }
        public byte Source { get; init; }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.