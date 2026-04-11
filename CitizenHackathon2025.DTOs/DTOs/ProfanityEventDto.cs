using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public sealed class ProfanityEventDto
    {
        public int? MessageId { get; set; }
        public string ContentPreview { get; set; } = string.Empty;
        public int Score { get; set; }
        public ToxicityLevel ToxicityLevel { get; set; }
        public IReadOnlyCollection<string> MatchedWords { get; set; } = Array.Empty<string>();
        public DateTime OccurredAtUtc { get; set; }
    }
}















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.