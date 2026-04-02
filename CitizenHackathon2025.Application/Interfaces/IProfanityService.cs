using CitizenHackathon2025.Domain.Models;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IProfanityService
    {
        /// <summary>
        /// Returns true if the content contains any banned or toxic words.
        /// </summary>
        bool ContainsProfanity(string content);

        /// <summary>
        /// Returns a normalized version of the content
        /// (lowercase, symbol cleanup, anti-obfuscation).
        /// </summary>
        string Normalize(string content);

        /// <summary>
        /// Returns a toxicity score (0 = clean, higher = more toxic).
        /// </summary>
        int GetToxicityScore(string content);

        /// <summary>
        /// Returns the list of detected banned words inside the content.
        /// </summary>
        IReadOnlyCollection<string> GetMatchedWords(string content);
        /// <summary>
        /// Returns a cleaned version of the content (masked words).
        /// </summary>
        string Sanitize(string content);
        Task<ProfanityAnalysisResult> AnalyzeAsync(string content, CancellationToken ct = default);
        Task<bool> ContainsProfanityAsync(string content, CancellationToken ct = default);
    }
}
