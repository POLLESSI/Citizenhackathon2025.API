namespace CitizenHackathon2025.Application.Intelligence.AlertFusion
{
    public sealed class AlertCluster
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string AlertKind { get; set; } = "Crowd";

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public byte Severity { get; set; }

        public int TotalSignals { get; set; }

        public List<long> SourceAlertIds { get; set; } = new();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.
