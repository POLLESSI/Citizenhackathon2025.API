using CitizenHackathon2025.Domain.Models;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IProfanityService
    {
        string Normalize(string content);

        Task<bool> ContainsProfanityAsync(string content, CancellationToken ct = default);

        Task<ProfanityAnalysisResult> AnalyzeAsync(string content, CancellationToken ct = default);

        bool ContainsProfanity(string content);

        int GetToxicityScore(string content);

        IReadOnlyCollection<string> GetMatchedWords(string content);

        string Sanitize(string content);
    }
}




















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.