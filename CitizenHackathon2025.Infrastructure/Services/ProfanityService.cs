using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Models;
using System.Text;
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

            // We remove the extraneous punctuation, but we keep the spaces to tokenize correctly.
            normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s]+", " ");
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

            var tokens = Regex
                .Split(normalized, @"\s+")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word.Word))
                    continue;

                var normalizedWord = string.IsNullOrWhiteSpace(word.NormalizedWord)
                    ? Normalize(word.Word)
                    : word.NormalizedWord;

                if (word.IsRegex)
                {
                    try
                    {
                        if (Regex.IsMatch(
                            normalized,
                            word.Word,
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                            TimeSpan.FromMilliseconds(150)))
                        {
                            matched.Add(word.Word);
                            score += Math.Max(1, word.Weight);
                        }
                    }
                    catch
                    {
                        // Invalid regex in base -> we ignore
                    }

                    continue;
                }

                // Exact match on token to avoid false positives like "con" in "consequuntur"
                if (tokens.Contains(normalizedWord))
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
            var normalized = Normalize(content);
            return !string.IsNullOrWhiteSpace(normalized) && GetMatchedWords(content).Count > 0;
        }

        public int GetToxicityScore(string content)
        {
            // simple sync version for local compatibility
            var normalized = Normalize(content);
            if (string.IsNullOrWhiteSpace(normalized))
                return 0;

            // Here we can't properly call the sync repository, so :
            // Either you remove this sync method,
            // Either you assume that only AnalyzeAsync is used.
            throw new NotSupportedException("Use AnalyzeAsync instead.");
        }

        public IReadOnlyCollection<string> GetMatchedWords(string content)
        {
            throw new NotSupportedException("Use AnalyzeAsync instead.");
        }

        public string Sanitize(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            return content;
        }
    }
}





















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.