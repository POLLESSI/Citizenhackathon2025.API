using CitizenHackathon2025.Contracts.Enums;

namespace CitizenHackathon2025.Domain.Entities
{
    public sealed class MessageTriageResult
    {
        public bool RequiresAdminReview { get; init; }

        public AdminMessageCategory Category { get; init; }

        public byte Priority { get; init; }

        public double Confidence { get; init; }

        public string ClassificationSource { get; init; } = "Rules";
    }
}


























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.