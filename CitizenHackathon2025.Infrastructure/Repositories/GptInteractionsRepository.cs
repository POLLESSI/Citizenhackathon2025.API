using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class GptInteractionsRepository : IGPTRepository
    {
#nullable disable
        private readonly IDbConnection _connection;
        private readonly IConfiguration _config;
        private readonly ILogger<GptInteractionsRepository> _logger;

        public GptInteractionsRepository(IDbConnection connection, IConfiguration config, ILogger<GptInteractionsRepository> logger)
        {
            _connection = connection;
            _config = config;
            _logger = logger;
        }

        // ======================
        // Helpers
        // ======================
        private static string NormalizePrompt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var t = s.Trim().Normalize(NormalizationForm.FormKC);
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ");
            return t.ToLowerInvariant();
        }

        private static string HmacSha256Hex(string input, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ======================
        // Suggestions
        // ======================

        public async Task SaveSuggestionAsync(Suggestion suggestion)
        {
            // Table/columns to be confirmed: "Suggestion" vs "Suggestions"
            const string sql = @"
                    INSERT INTO Suggestion (User_Id, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason, Active, LocationName, EventId, ForecastId, TrafficId)
                    VALUES (@User_Id, @DateSuggestion, @OriginalPlace, @SuggestedAlternatives, @Reason, 1, @LocationName, @EventId, @ForecastId, @TrafficId);";

            await _connection.ExecuteAsync(sql, new
            {
                suggestion.User_Id,
                suggestion.DateSuggestion,
                suggestion.OriginalPlace,
                suggestion.SuggestedAlternatives,
                suggestion.Reason,
                suggestion.LocationName,
                suggestion.EventId,
                suggestion.ForecastId,
                suggestion.TrafficId
            });
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            const string sql = @"SELECT * FROM Suggestion WHERE Active = 1 ORDER BY DateSuggestion DESC;";
            return await _connection.QueryAsync<Suggestion>(sql);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id)
        {
            const string sql = @"SELECT * FROM Suggestion WHERE EventId = @Id AND Active = 1 ORDER BY DateSuggestion DESC;";
            return await _connection.QueryAsync<Suggestion>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id)
        {
            const string sql = @"SELECT * FROM Suggestion WHERE ForecastId = @Id AND Active = 1 ORDER BY DateSuggestion DESC;";
            return await _connection.QueryAsync<Suggestion>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id)
        {
            const string sql = @"SELECT * FROM Suggestion WHERE TrafficId = @Id AND Active = 1 ORDER BY DateSuggestion DESC;";
            return await _connection.QueryAsync<Suggestion>(sql, new { Id = id });
        }

        public async Task DeleteSuggestionAsync(int id)
        {
            const string sql = @"UPDATE Suggestion SET Active = 0, DateDeleted = SYSUTCDATETIME() WHERE Id = @Id;";
            var n = await _connection.ExecuteAsync(sql, new { Id = id });
            if (n == 0)
                throw new KeyNotFoundException($"Suggestion #{id} inexistante ou déjà supprimée.");
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByPlaceCrowdAsync(string placeName)
        {
            const string sql = @"
                            SELECT s.*
                            FROM Suggestion s
                            INNER JOIN CrowdInfo c ON s.OriginalPlace = c.LocationName AND c.Active = 1
                            INNER JOIN Place p     ON p.Name = c.LocationName AND p.Active = 1
                            WHERE LOWER(p.Name) = LOWER(@PlaceName) AND s.Active = 1
                            ORDER BY s.DateSuggestion DESC;";
            return await _connection.QueryAsync<Suggestion>(sql, new { PlaceName = placeName });
        }

        public async Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetSuggestionsGroupedByPlaceAsync(
            string? typeFilter = null, bool? indoorFilter = null, DateTime? sinceDate = null)
        {
            const string baseSql = @"
                                SELECT 
                                    s.OriginalPlace                                     AS PlaceName,
                                    MAX(p.Type)                                         AS Type,
                                    CAST(MAX(CAST(p.Indoor AS TINYINT)) AS BIT)         AS Indoor,
                                    MAX(p.Latitude)                                     AS Latitude,
                                    MAX(p.Longitude)                                    AS Longitude,
                                    MAX(c.CrowdLevel)                                   AS CrowdLevel,
                                    COUNT(*)                                            AS SuggestionCount,
                                    MAX(s.DateSuggestion)                               AS LastSuggestedAt
                                FROM Suggestion s
                                LEFT JOIN Place p    ON s.OriginalPlace = p.Name AND p.Active = 1
                                LEFT JOIN CrowdInfo c ON c.LocationName = s.OriginalPlace AND c.Active = 1
                                WHERE s.Active = 1
                                /**WHERE_FILTER**/
                                GROUP BY s.OriginalPlace
                                ORDER BY LastSuggestedAt DESC;";

            var filters = new List<string>();
            var param = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                filters.Add("p.Type = @TypeFilter");
                param.Add("TypeFilter", typeFilter);
            }
            if (indoorFilter.HasValue)
            {
                filters.Add("p.Indoor = @IndoorFilter");
                param.Add("IndoorFilter", indoorFilter.Value);
            }
            if (sinceDate.HasValue)
            {
                filters.Add("s.DateSuggestion >= @SinceDate");
                param.Add("SinceDate", sinceDate.Value);
            }

            var whereClause = filters.Count > 0 ? "AND " + string.Join(" AND ", filters) : string.Empty;
            var sql = baseSql.Replace("/**WHERE_FILTER**/", whereClause);

            return await _connection.QueryAsync<SuggestionGroupedByPlaceDTO>(sql, param);
        }

        // ======================
        // Interactions GPT
        // ======================

        public async Task SaveInteractionAsync(GPTInteraction interaction)
        {
            var pepper = _config["Security:PromptHashPepper"];
            if (string.IsNullOrEmpty(pepper))
                throw new InvalidOperationException("Missing Security:PromptHashPepper in configuration.");

            var normalized = NormalizePrompt(interaction.Prompt);
            var promptHash = HmacSha256Hex(normalized, pepper);

            const string sql = @"
                            MERGE [dbo].[GptInteractions] WITH (HOLDLOCK) AS t
                            USING (SELECT @PromptHash AS PromptHash) AS s
                            ON (t.PromptHash = s.PromptHash)
                            WHEN MATCHED THEN
                                UPDATE SET Response = @Response, CreatedAt = SYSUTCDATETIME(), Active = 1
                            WHEN NOT MATCHED THEN
                                INSERT (Prompt, PromptHash, Response, CreatedAt, Active)
                                VALUES (@Prompt, @PromptHash, @Response, SYSUTCDATETIME(), 1);";

            await _connection.ExecuteAsync(sql, new
            {
                Prompt = interaction.Prompt,
                PromptHash = promptHash,
                Response = interaction.Response
            });
        }

        public async Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
        {
            const string sql = @"SELECT * FROM [GptInteractions] WHERE Active = 1 ORDER BY CreatedAt DESC;";
            return await _connection.QueryAsync<GPTInteraction>(sql);
        }

        public async Task<GPTInteraction?> GetByIdAsync(int id)
        {
            const string sql = @"SELECT * FROM [GptInteractions] WHERE Id = @Id AND Active = 1;";
            return await _connection.QueryFirstOrDefaultAsync<GPTInteraction>(sql, new { Id = id });
        }

        // Simulation (remplace par appel IA réel si besoin)
        public async Task<string> AskAsync(string question)
        {
            string simulatedResponse = $"(Simulated GPT Response) You asked : \"{question}\"";
            await SaveInteractionAsync(new GPTInteraction
            {
                Prompt = question,
                Response = simulatedResponse
            });
            return simulatedResponse;
        }

        public async Task<bool> DeactivateInteractionAsync(int id)
        {
            const string sql = @"
                            UPDATE [GptInteractions]
                            SET Active = 0
                            WHERE Id = @Id AND Active = 1;";
            var n = await _connection.ExecuteAsync(sql, new { Id = id });
            return n > 0;
        }

        public async Task<int> ArchivePastGptInteractionsAsync()
        {
            const string sql = @"
                            UPDATE [GptInteractions]
                            SET [Active] = 0
                            WHERE [Active] = 1
                              AND [CreatedAt] < DATEADD(DAY, -1, SYSUTCDATETIME());";
            try
            {
                var affected = await _connection.ExecuteAsync(sql);
                _logger.LogInformation("{Count} GPT interaction(s) archived.", affected);
                return affected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving past GPT interactions.");
                return 0;
            }
        }
        public async Task<GPTInteraction?> UpsertInteractionAsync(GPTInteraction interaction)
        {
            var pepper = _config["Security:PromptHashPepper"];
            if (string.IsNullOrWhiteSpace(pepper))
                throw new InvalidOperationException("Missing Security:PromptHashPepper in configuration.");

            // reuse your helpers (NormalizePrompt + HmacSha256Hex)
            var normalized = NormalizePrompt(interaction.Prompt);
            var promptHash = HmacSha256Hex(normalized, pepper);

            const string sql = @"EXEC dbo.sp_GptInteraction_Upsert @Prompt, @PromptHash, @Response;";

            var parameters = new DynamicParameters();
            parameters.Add("@Prompt", interaction.Prompt);
            parameters.Add("@PromptHash", promptHash);
            parameters.Add("@Response", interaction.Response);

            return await _connection.QuerySingleOrDefaultAsync<GPTInteraction>(sql, parameters);
        }
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.