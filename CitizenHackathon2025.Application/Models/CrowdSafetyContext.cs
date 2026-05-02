// CitizenHackathon2025.Application/Models/CrowdSafetyContext.cs
namespace CitizenHackathon2025.Application.Models
{
    public sealed class CrowdSafetyContext
    {
        public int ActiveConnections { get; set; }
        public int UniqueDevices { get; set; }
        public int? MaxCapacity { get; set; }
        public int? BaselineConnections { get; set; }

        public bool IsRural { get; set; }
        public bool IsNight { get; set; }
        public bool IsKnownEvent { get; set; }
        public bool IsSensitiveZone { get; set; }
        public bool IsPersistent { get; set; }
    }
}












































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.