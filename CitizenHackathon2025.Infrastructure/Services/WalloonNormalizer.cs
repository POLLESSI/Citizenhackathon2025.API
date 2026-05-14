using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Language;

namespace CitizenHackathon2025.Infrastructure.Services;

public sealed class WalloonNormalizer : IWalloonNormalizer
{
    private static readonly Dictionary<string, string> Lexicon = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cwè"] = "quoi",
        ["cwé"] = "quoi",
        ["dji"] = "je",
        ["dj'"] = "je",
        ["vos"] = "vous",
        ["oyi"] = "oui",
        ["nenni"] = "non"
    };

    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim();

        foreach (var item in Lexicon)
        {
            normalized = normalized.Replace(
                item.Key,
                item.Value,
                StringComparison.OrdinalIgnoreCase);
        }

        return normalized;
    }

    public LanguageConfidenceResult EstimateConfidence(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new LanguageConfidenceResult
            {
                WalloonConfidence = 0,
                FallbackToFrench = true,
                FinalLanguageUsed = "fr-FR"
            };
        }

        var hits = Lexicon.Keys.Count(k =>
            input.Contains(k, StringComparison.OrdinalIgnoreCase));

        var confidence = Math.Min(1.0, hits / 3.0);

        return new LanguageConfidenceResult
        {
            WalloonConfidence = confidence,
            FallbackToFrench = confidence < 0.35,
            FinalLanguageUsed = confidence < 0.35 ? "fr-FR" : "wa-central"
        };
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.