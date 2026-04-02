using CitizenHackathon2025.Domain.Enums;

namespace CitizenHackathon2025.Domain.Models
{
    public sealed class ProfanityAnalysisResult
    {
        public bool HasProfanity { get; set; }
        public int Score { get; set; }
        public ToxicityLevel ToxicityLevel { get; set; }
        public string OriginalContent { get; set; } = string.Empty;
        public string NormalizedContent { get; set; } = string.Empty;
        public IReadOnlyCollection<string> MatchedWords { get; set; } = Array.Empty<string>();
        public bool ShouldReject { get; set; }
        public bool ShouldFlagForReview { get; set; }
    }
}