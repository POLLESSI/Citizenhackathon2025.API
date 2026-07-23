using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class PlaceNameResolver: IPlaceNameResolver
    {
        private readonly DbConnectionFactory _connectionFactory;
        private readonly IPlaceRepository _placeRepository;
        private readonly ILogger<PlaceNameResolver> _logger;

        public PlaceNameResolver(DbConnectionFactory connectionFactory, IPlaceRepository placeRepository, ILogger<PlaceNameResolver> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _placeRepository = placeRepository ?? throw new ArgumentNullException(nameof(placeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Place?> ResolveAsync(string prompt, string? languageCode, CancellationToken ct = default)
        {
            _logger.LogWarning("[PLACE RESOLVER] Starting. " + "Language={Language}; Prompt={Prompt}", languageCode, prompt);

            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            var places = (await _placeRepository.GetActivePlacesAsync(ct))
                .Where(place => place is not null && !string.IsNullOrWhiteSpace(place.Name))
                .ToList();

            if (places.Count == 0)
                return null;

            var normalizedPrompt = NormalizeSearchText(prompt);

            /*
             * Initial search :
             * canonical name from dbo.Place.
             *
             * This works directly for :
             * Stavelot, Bouillon, Namur, Dinant, etc.
             */
            var canonicalMatch = places
                .Select(place => new
                {
                    Place = place,
                    NormalizedName = NormalizeSearchText(place.Name)
                })
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.NormalizedName))
                .Where(candidate => normalizedPrompt.Contains(candidate.NormalizedName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(candidate => candidate.NormalizedName.Length)
                .Select(candidate => candidate.Place)
                .FirstOrDefault();

            if (canonicalMatch is not null)
            {
                _logger.LogInformation(
                    "[PLACE RESOLVER] Canonical match. " +
                    "PromptLanguage={Language}; PlaceId={PlaceId}; " +
                    "Place={Place}",
                    languageCode,
                    canonicalMatch.Id,
                    canonicalMatch.Name);

                return canonicalMatch;
            }

            /*
             * Second search :
             * linguistic variants from dbo.PlaceAlias.
             */
            const string sql = """
                            SELECT
                                pa.PlaceId,
                                pa.Alias,
                                pa.LanguageCode,
                                pa.NormalizedAlias
                            FROM dbo.PlaceAlias pa
                            INNER JOIN dbo.Place p
                                ON p.Id = pa.PlaceId
                            WHERE pa.Active = 1
                              AND p.Active = 1;
                            """;

            IReadOnlyList<PlaceAliasRow> aliases;

            try
            {
                using var connection = await OpenConnectionAsync(ct);

                var rows = await connection.QueryAsync<PlaceAliasRow>(new CommandDefinition(sql, cancellationToken: ct));

                aliases = rows.ToList();

                _logger.LogWarning("[PLACE RESOLVER] Aliases loaded. Count={AliasCount}", aliases.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PLACE RESOLVER] Unable to read dbo.PlaceAlias. " + "Language={Language}; Prompt={Prompt}", languageCode, prompt);

                throw;
            }

            var aliasMatch = aliases
                .Select(alias => new
                {
                    Alias = alias,
                    SearchValue = NormalizeSearchText(string.IsNullOrWhiteSpace(alias.NormalizedAlias) ? alias.Alias : alias.NormalizedAlias)
                })
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.SearchValue))
                .Where(candidate => normalizedPrompt.Contains(candidate.SearchValue, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(candidate => candidate.SearchValue.Length)
                .ThenByDescending(candidate => LanguageMatches(candidate.Alias.LanguageCode, languageCode))
                .Select(candidate => candidate.Alias)
                .FirstOrDefault();

            if (aliasMatch is null)
            {
                _logger.LogWarning("[PLACE RESOLVER] No place found. " + "Language={Language}; Prompt={Prompt}", languageCode, prompt);

                return null;
            }

            var resolvedPlace = places.FirstOrDefault(place => place.Id == aliasMatch.PlaceId);

            _logger.LogWarning("[PLACE RESOLVER] Alias matched. " + "Alias={Alias}; PlaceId={PlaceId}; " + "CanonicalPlace={CanonicalPlace}", aliasMatch.Alias, aliasMatch.PlaceId, resolvedPlace?.Name);

            return resolvedPlace;
        }

        private async Task<IDbConnection>OpenConnectionAsync(CancellationToken ct)
        {
            var connection = _connectionFactory.CreateConnection();

            if (connection is SqlConnection sqlConnection)
            {
                await sqlConnection.OpenAsync(ct);
                return sqlConnection;
            }

            connection.Open();
            return connection;
        }

        private static bool LanguageMatches(string? aliasLanguage, string? requestedLanguage)
        {
            if (string.IsNullOrWhiteSpace(aliasLanguage) || string.IsNullOrWhiteSpace(requestedLanguage))
            {
                return false;
            }

            if (string.Equals(aliasLanguage, requestedLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            var aliasPrimary = aliasLanguage.Split('-')[0];
            var requestedPrimary = requestedLanguage.Split('-')[0];

            return string.Equals(aliasPrimary, requestedPrimary, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSearchText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var decomposed = value
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(decomposed.Length);

            foreach (var character in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return Regex.Replace(builder.ToString(), @"[^\p{L}\p{N}]+", " ").Trim();
        }

        private sealed class PlaceAliasRow
        {
            public int PlaceId { get; init; }
            public string Alias { get; init; } = string.Empty;
            public string? LanguageCode { get; init; }
            public string NormalizedAlias { get; init; } = string.Empty;
        }
    }
}













































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.