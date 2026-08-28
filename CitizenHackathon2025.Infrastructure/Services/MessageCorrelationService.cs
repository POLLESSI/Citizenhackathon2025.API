using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class MessageCorrelationService : IMessageCorrelationService
    {
        private const double MinimumScore = 70d;
        private const double MinimumMargin = 10d;

        private readonly IDbConnection _db;
        private readonly ILogger<MessageCorrelationService> _logger;

        /*
         * Important :
         *
         * These words may appear in the message,
         * but do NOT constitute sufficiently discriminating identifiers of a place or event.
         *
         * This precisely eliminates the false positives
         * "pas" and "des" currently observed.
         */
        private static readonly HashSet<string> StopWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // FR
                "alors",
                "apres",
                "avec",
                "avant",
                "aux",
                "avoir",
                "car",
                "ceci",
                "cela",
                "ces",
                "chez",
                "comme",
                "dans",
                "des",
                "donc",
                "elle",
                "elles",
                "encore",
                "entre",
                "est",
                "etre",
                "fait",
                "faire",
                "ils",
                "leurs",
                "mais",
                "mes",
                "moi",
                "mon",
                "nos",
                "notre",
                "nous",
                "pas",
                "plus",
                "pour",
                "que",
                "quel",
                "quelle",
                "qui",
                "sans",
                "ses",
                "son",
                "sont",
                "sous",
                "sur",
                "tes",
                "toi",
                "ton",
                "tous",
                "tout",
                "tres",
                "trop",
                "une",
                "vos",
                "votre",
                "vous",

                // OutZen context words,
                // useful for intent but not for identifying
                // an entity.
                "monde",
                "foule",
                "crowd",
                "trafic",
                "traffic",
                "route",
                "meteo",
                "weather",
                "pluie",
                "danger",
                "alerte",
                "event",
                "evenement",
                "ville",
                "commune",
                "village",
                "centre",
                "place",
                "rue",
                "route",
                "avenue",
                "boulevard",
                "quartier",

                // EN
                "the",
                "and",
                "with",
                "from",
                "this",
                "that",
                "there",
                "have",
                "has",
                "for",
                "not",

                // NL
                "het",
                "een",
                "van",
                "voor",
                "met",
                "niet",
                "dat",
                "deze"
            };

        /*
         * Generic entity words are useful when several
         * tokens match, but must never identify an entity
         * by themselves.
         *
         * IMPORTANT:
         * tokens have already passed through Normalize(),
         * therefore accented variants are unnecessary here.
         *
         * château -> chateau
         * musée   -> musee
         * église  -> eglise
         */
        private static readonly HashSet<string> GenericEntityTokens =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "centre",
                    "ville",
                    "commune",
                    "village",

                    "centre",
                    "quartier",

                    "place",
                    "rue",
                    "route",
                    "avenue",
                    "boulevard",

                    "site",
                    "parc",
                    "lac",
                    "barrage",

                    "chateau",
                    "musee",
                    "eglise",

                    "gare",
                    "hall"
                };

        public MessageCorrelationService(IDbConnection db, ILogger<MessageCorrelationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<UserMessage> CorrelateAsync(UserMessage raw, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(raw);

            var content = raw.Content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                return ClearCorrelation(raw);
            }

            var normalizedContent = Normalize(content);
            var tokens = ExtractSearchTokens(content);

            if (tokens.Count == 0)
            {
                _logger.LogInformation("[MESSAGE-CORRELATION] " + "No significant tokens.");

                return ClearCorrelation(raw);
            }

            var intent = DetectIntent(normalizedContent);
            var candidates = await LoadCandidatesAsync(tokens, ct);

            /*
             * A CrowdInfo can have multiple observations
             * for the same place.
             *
             * We avoid having a duplicate appear as the
             * second best candidate and artificially make
             * the response ambiguous.
             */

            var distinctCandidates =
                candidates
                    .GroupBy(x => $"{x.SourceType}|" + Normalize(x.RelatedName), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

            var uniqueEntityNames =
                distinctCandidates
                    .GroupBy(candidate => Normalize(candidate.RelatedName), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();

            var candidateTokenFrequency =
                uniqueEntityNames
                    .SelectMany(candidate => ExtractSearchTokens(candidate.RelatedName).Distinct(StringComparer.OrdinalIgnoreCase))
                    .GroupBy(token => token, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            var scored = distinctCandidates
                .Select(candidate => ScoreCandidate(candidate, normalizedContent, tokens, intent, candidateTokenFrequency))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.ExactPhrase)
                .ThenByDescending(x => x.MatchedTokens)
                .ToList();

            if (scored.Count == 0)
            {
                _logger.LogInformation("[MESSAGE-CORRELATION] " + "No viable candidate. Content={Content}", content);

                return ClearCorrelation(raw);
            }

            var best = scored[0];
            var second = scored.Count > 1 ? scored[1] : null;

            /*
             * Reject low score correlations.
             */
            if (best.Score < MinimumScore)
            {
                _logger.LogInformation(
                    "[MESSAGE-CORRELATION] " +
                    "Rejected low score. " +
                    "Best={Best}; Score={Score}",
                    best.Candidate.RelatedName,
                    best.Score);

                return ClearCorrelation(raw);
            }

            /*
             * Also reject ambiguous situations.
             *
             * For example, two places with nearly
             * the same name.
             */
            if (second is not null && best.Score - second.Score < MinimumMargin)
            {
                _logger.LogInformation(
                    "[MESSAGE-CORRELATION] " +
                    "Ambiguous correlation. " +
                    "First={First} ({FirstScore}); " +
                    "Second={Second} ({SecondScore})",
                    best.Candidate.RelatedName,
                    best.Score,
                    second.Candidate.RelatedName,
                    second.Score);

                return ClearCorrelation(raw);
            }

            raw.SourceType = best.Candidate.SourceType;
            raw.SourceId = best.Candidate.Id;
            raw.RelatedName = best.Candidate.RelatedName;
            raw.Latitude = best.Candidate.Latitude;
            raw.Longitude = best.Candidate.Longitude;

            _logger.LogInformation(
                "[MESSAGE-CORRELATION] " +
                "Correlated. " +
                "SourceType={SourceType}; " +
                "SourceId={SourceId}; " +
                "Name={Name}; " +
                "Score={Score:F1}; " +
                "ExactPhrase={ExactPhrase}; " +
                "UniquePartial={UniquePartial}; " +
                "MatchedTokens={MatchedTokens}; " +
                "Intent={Intent}",
                raw.SourceType,
                raw.SourceId,
                raw.RelatedName,
                best.Score,
                best.ExactPhrase,
                best.UniquePartialName,
                best.MatchedTokens,
                intent);            

            return raw;
        }

        private async Task<IReadOnlyList<CorrelationCandidate>> LoadCandidatesAsync(IReadOnlyCollection<string> tokens, CancellationToken ct)
        {
            var parameters = new DynamicParameters();
            var tokenList = tokens.OrderByDescending(x => x.Length).Take(10).ToList();

            for (var i = 0; i < tokenList.Count; i++)
            {
                parameters.Add($"p{i}", $"%{tokenList[i]}%");
            }

            var crowdWhere = BuildLikeClause("ci.LocationName", tokenList.Count);
            var eventWhere = BuildLikeClause("e.[Name]", tokenList.Count);
            var placeWhere = BuildLikeClause("p.[Name]", tokenList.Count);

            var sql = $"""
                SELECT *
                FROM
                (
                    SELECT TOP (40)
                        SourceType = 'Crowd',
                        ci.Id,
                        RelatedName = ci.LocationName,
                        Latitude = CAST(ci.Latitude AS decimal(18,8)),
                        Longitude = CAST(ci.Longitude AS decimal(18,8))
                    FROM dbo.CrowdInfo ci
                    WHERE ci.Active = 1
                      AND ci.LocationName IS NOT NULL
                      AND ({crowdWhere})
                    ORDER BY ci.[Timestamp] DESC
                ) crowd

                UNION ALL

                SELECT *
                FROM
                (
                    SELECT TOP (40)
                        SourceType = 'Event',
                        e.Id,
                        RelatedName = e.[Name],
                        Latitude = CAST(e.Latitude AS decimal(18,8)),
                        Longitude = CAST(e.Longitude AS decimal(18,8))
                    FROM dbo.Event e
                    WHERE e.Active = 1
                      AND e.[Name] IS NOT NULL
                      AND ({eventWhere})
                    ORDER BY e.DateEvent DESC
                ) events

                UNION ALL

                SELECT *
                FROM
                (
                    SELECT TOP (40)
                        SourceType = 'Place',
                        p.Id,
                        RelatedName = p.[Name],
                        Latitude = CAST(p.Latitude AS decimal(18,8)),
                        Longitude = CAST(p.Longitude AS decimal(18,8))
                    FROM dbo.Place p
                    WHERE p.Active = 1
                      AND p.[Name] IS NOT NULL
                      AND ({placeWhere})
                    ORDER BY p.Id DESC
                ) places;
                """;

            var rows = await _db.QueryAsync<CorrelationCandidate>(
                    new CommandDefinition(
                        sql,
                        parameters,
                        cancellationToken: ct));

            return rows.ToList();
        }

        private static string BuildLikeClause(string column, int count)
        {
            return string.Join(" OR ", Enumerable.Range(0, count).Select(i => $"{column} " + "COLLATE Latin1_General_100_CI_AI " + $"LIKE @p{i}"));
        }

        private static ScoredCandidate ScoreCandidate(CorrelationCandidate candidate, string normalizedContent, IReadOnlyCollection<string> messageTokens, MessageIntent intent, IReadOnlyDictionary<string, int> candidateTokenFrequency)
        {
            var normalizedName = Normalize(candidate.RelatedName);

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return ScoredCandidate.Empty(candidate);
            }

            var nameTokens = ExtractSearchTokens(candidate.RelatedName);

            if (nameTokens.Count == 0)
            {
                return ScoredCandidate.Empty(
                    candidate);
            }

            var messageSet = messageTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matchedTokens = nameTokens.Where(messageSet.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var matched = matchedTokens.Count;
            var exactPhrase = ContainsPhrase(normalizedContent, normalizedName);
            var singleStrongName = nameTokens.Count == 1 && matched == 1 && IsStrongEntityToken(matchedTokens[0]);
            var uniqueStrongPartialName = matched == 1 && IsStrongEntityToken(matchedTokens[0]) && candidateTokenFrequency
                .TryGetValue(matchedTokens[0], out var tokenFrequency) && tokenFrequency == 1;

            var multiTokenMatch = matched >= 2;
            var viable = exactPhrase || singleStrongName || uniqueStrongPartialName || multiTokenMatch;

            if (!viable)
            {
                return ScoredCandidate.Empty(candidate);
            }

            var coverage = (double)matched / nameTokens.Count;
            var score = 0d;

            if (exactPhrase)
            {
                score += 100d;
            }

            score += coverage * 55d;
            score += matched * 10d;

            if (singleStrongName)
            {
                score += 25d;
            }

            /*
             * A unique distinctive token constitutes
             * a strong signal, but slightly less
             * strong than the full name.
             */
            if (uniqueStrongPartialName)
            {
                score += 45d;
            }

            /*
             * The intent only serves
             * as arbitration.
             *
             * It can never make a lexically false
             * candidate viable.
             */
            score += GetIntentBonus(candidate.SourceType, intent);

            return new ScoredCandidate
            {
                Candidate = candidate,
                Score = score,
                ExactPhrase = exactPhrase,
                MatchedTokens = matched,
                UniquePartialName = uniqueStrongPartialName
            };
        }

        private static double GetIntentBonus(string sourceType, MessageIntent intent)
        {
            return intent switch
            {
                MessageIntent.Crowd when sourceType.Equals("Crowd", StringComparison.OrdinalIgnoreCase) => 15d,
                MessageIntent.Event when sourceType.Equals("Event", StringComparison.OrdinalIgnoreCase) => 12d,
                _ => 0d
            };
        }

        private static MessageIntent DetectIntent(string normalized)
        {
            if (ContainsAny(normalized, "foule", "monde", "affluence", "bonde", "sature", "surpeuple", "file attente", "bazar", "bazard", "coince", "bloque", "embouteille"))
            {
                return MessageIntent.Crowd;
            }

            if (ContainsAny(normalized, "trafic", "traffic", "bouchon", "route bloquee", "accident", "deviation"))
            {
                return MessageIntent.Traffic;
            }

            if (ContainsAny(normalized, "concert", "festival", "evenement", "event", "spectacle"))
            {
                return MessageIntent.Event;
            }

            if (ContainsAny(normalized, "danger", "emeute", "incendie", "evacuation", "inondation", "alerte"))
            {
                return MessageIntent.Safety;
            }

            return MessageIntent.Other;
        }

        private static bool IsStrongEntityToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (token.Length < 5)
                return false;

            if (StopWords.Contains(token))
                return false;

            if (GenericEntityTokens.Contains(token))
                return false;

            return token.Any(char.IsLetter);
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            return values.Any(x => ContainsPhrase(text, Normalize(x)));
        }

        private static bool ContainsPhrase(string normalizedText, string normalizedPhrase)
        {
            var haystack = $" {normalizedText} ";
            var needle = $" {normalizedPhrase} ";

            return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string>ExtractSearchTokens(string content)
        {
            var normalized = Normalize(content);

            return normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length >= 3)
                .Where(x => !StopWords.Contains(x))
                .Where(x => x.Any(char.IsLetter))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Length)
                .Take(12)
                .ToList();
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var decomposed = value.Trim().Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(' ');
                }
            }

            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private static UserMessage ClearCorrelation(UserMessage message)
        {
            message.SourceType = "Other";
            message.SourceId = null;
            message.RelatedName = null;
            message.Latitude = null;
            message.Longitude = null;

            return message;
        }

        private enum MessageIntent
        {
            Other,
            Crowd,
            Traffic,
            Event,
            Safety
        }

        private sealed class CorrelationCandidate
        {
            public string SourceType { get; set; } = "";
            public int Id { get; set; }

            /*
             * Canonical entity name.
             *
             * Example:
             * Fosses-la-Ville
             */
            public string RelatedName { get; set; } = "";

            /*
             * Text which actually caused the candidate
             * to be retrieved.
             *
             * It can be the canonical name
             * OR one of its aliases.
             *
             * Example:
             * Fosses
             */
            public string MatchName { get; set; } = "";
            public bool MatchedByAlias { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
        }

        private sealed class ScoredCandidate
        {
            public CorrelationCandidate Candidate { get; init; }= default!;
            public double Score { get; init; }
            public bool ExactPhrase { get; init; }
            public bool UniquePartialName { get; init; }
            public int MatchedTokens { get; init; }
            public static ScoredCandidate Empty(CorrelationCandidate candidate)
            {
                return new ScoredCandidate
                {
                    Candidate = candidate,
                    Score = 0,
                    ExactPhrase = false,
                    MatchedTokens = 0,
                    UniquePartialName = false
                };
            }
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.