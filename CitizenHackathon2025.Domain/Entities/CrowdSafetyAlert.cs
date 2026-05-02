namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class CrowdSafetyAlert
    {
        public long Id { get; set; }

        public int AntennaId { get; set; }
        public int? EventId { get; set; }

        public byte Severity { get; set; }
        public string Status { get; set; } = "PendingValidation";

        public int ActiveConnections { get; set; }
        public int UniqueDevices { get; set; }
        public int? BaselineConnections { get; set; }

        public bool IsRural { get; set; }
        public bool IsNight { get; set; }
        public bool IsKnownEvent { get; set; }
        public bool IsSensitiveZone { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public string Title { get; set; } = "";
        public string Message { get; set; } = "";

        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ValidatedAtUtc { get; set; }
        public int? ValidatedByUserId { get; set; }
        public DateTime? LastReminderAtUtc { get; set; }
        public int ReminderCount { get; set; }

        public bool Active { get; set; } = true;
    }
}










































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.