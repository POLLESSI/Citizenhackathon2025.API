using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Models;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class ProfanityService : IProfanityService
    {
        private readonly IProfanityRepository _repo;

        public ProfanityService(IProfanityRepository repo)
        {
            _repo = repo;
        }

        public string Normalize(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var normalized = content
                .ToLowerInvariant()
                .Replace("0", "o")
                .Replace("1", "i")
                .Replace("3", "e")
                .Replace("@", "a")
                .Replace("$", "s")
                .Replace("!", "i");

            normalized = Regex.Replace(normalized, @"[\.\-_\*]+", "");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        public async Task<bool> ContainsProfanityAsync(string content, CancellationToken ct = default)
        {
            var result = await AnalyzeAsync(content, ct);
            return result.HasProfanity;
        }

        public async Task<ProfanityAnalysisResult> AnalyzeAsync(string content, CancellationToken ct = default)
        {
            var normalized = Normalize(content);
            var words = await _repo.GetAllActiveAsync(ct);

            var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var score = 0;

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word.NormalizedWord))
                    continue;

                if (word.IsRegex)
                {
                    if (Regex.IsMatch(normalized, word.NormalizedWord, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                    {
                        matched.Add(word.Word);
                        score += Math.Max(1, word.Weight);
                    }

                    continue;
                }

                if (normalized.Contains(word.NormalizedWord, StringComparison.OrdinalIgnoreCase))
                {
                    matched.Add(word.Word);
                    score += Math.Max(1, word.Weight);
                }
            }

            var level = score switch
            {
                0 => ToxicityLevel.Clean,
                <= 2 => ToxicityLevel.Low,
                <= 4 => ToxicityLevel.Medium,
                <= 7 => ToxicityLevel.High,
                _ => ToxicityLevel.Critical
            };

            return new ProfanityAnalysisResult
            {
                OriginalContent = content ?? string.Empty,
                NormalizedContent = normalized,
                HasProfanity = matched.Count > 0,
                Score = score,
                ToxicityLevel = level,
                MatchedWords = matched.ToArray(),
                ShouldReject = level >= ToxicityLevel.High,
                ShouldFlagForReview = level is ToxicityLevel.Medium or ToxicityLevel.High
            };
        }

        public bool ContainsProfanity(string content)
        {
            throw new NotImplementedException();
        }

        public int GetToxicityScore(string content)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<string> GetMatchedWords(string content)
        {
            throw new NotImplementedException();
        }

        public string Sanitize(string content)
        {
            throw new NotImplementedException();
        }
    }
}
