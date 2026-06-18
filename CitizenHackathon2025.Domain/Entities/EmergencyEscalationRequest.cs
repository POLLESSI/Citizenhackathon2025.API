namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class EmergencyEscalationRequest
    {
        public long Id { get; set; }
        public long DisasterAlertId { get; set; }
        public string TargetService { get; set; } = "Multi";
        public string Status { get; set; } = "PendingOperatorReview";
        public string PayloadJson { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public int? ReviewedByUserId { get; set; }
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.