namespace CitizenHackathon2025.DTOs.DTOs.Antennas
{
    public sealed class AntennaPingDTO
    {
        public int AntennaId { get; init; }

        // Optional: if WEP sends its UTC. Otherwise, ignore it and use DateTime.UtcNow from the API.
        public DateTime? Utc { get; init; }
    }
}



















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.