namespace CitizenHackathon2025.Domain.Language
{
    public sealed class WalloonLanguageProfile
    {
        public string Variant { get; init; } = "Central";

        public bool HybridMode { get; init; } = true;

        public bool AddFrenchClarification { get; init; } = true;

        public bool AllowFrenchFallbackWords { get; init; } = true;

        public bool StrictVocabulary { get; init; }

        public IReadOnlyList<string> PreferredLexicon { get; init; } = [];
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.