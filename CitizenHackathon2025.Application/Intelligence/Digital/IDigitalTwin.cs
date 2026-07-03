namespace CitizenHackathon2025.Application.Intelligence.Digital
{
    public interface IDigitalTwin
    {
        Task<DigitalTwinSnapshot> GetCurrentStateAsync(CancellationToken ct = default);
    }

    public sealed class DigitalTwinSnapshot
    {
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string Scope { get; set; } = "Wallonia";
        public int ActiveZones { get; set; }
        public int ActiveIncidents { get; set; }
        public string Status { get; set; } = "Initialized";
    }
}










































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.