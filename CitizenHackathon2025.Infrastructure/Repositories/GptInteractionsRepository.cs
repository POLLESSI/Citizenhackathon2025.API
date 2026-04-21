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
    public sealed class GptInteractionsRepository : IGptInteractionRepository
    {
#nullable disable
        private readonly IDbConnection _connection;
        private readonly IConfiguration _config;
        private readonly ILogger<GptInteractionsRepository> _logger;

        public GptInteractionsRepository(IDbConnection connection, IConfiguration config, ILogger<GptInteractionsRepository> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ======================
        // Helpers
        // ======================

        private static string NormalizePrompt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

            return normalized.ToLowerInvariant();
        }

        private static string HmacSha256Hex(string input, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));

            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        private string ComputePromptHash(string? prompt)
        {
            var pepper = _config["Security:PromptHashPepper"];
            if (string.IsNullOrWhiteSpace(pepper))
                throw new InvalidOperationException("Missing Security:PromptHashPepper in configuration.");

            var normalized = NormalizePrompt(prompt);
            return HmacSha256Hex(normalized, pepper);
        }

        // ======================
        // Suggestions
        // ======================

        public async Task SaveSuggestionAsync(Suggestion suggestion)
        {
            ArgumentNullException.ThrowIfNull(suggestion);

            const string sql = @"
                            INSERT INTO Suggestion
                            (
                                User_Id,
                                DateSuggestion,
                                OriginalPlace,
                                SuggestedAlternatives,
                                Reason,
                                Active,
                                LocationName,
                                EventId,
                                ForecastId,
                                TrafficId
                            )
                            VALUES
                            (
                                @User_Id,
                                @DateSuggestion,
                                @OriginalPlace,
                                @SuggestedAlternatives,
                                @Reason,
                                1,
                                @LocationName,
                                @EventId,
                                @ForecastId,
                                @TrafficId
                            );";

            var parameters = new DynamicParameters();
            parameters.Add("@User_Id", suggestion.User_Id, DbType.Int32);
            parameters.Add("@DateSuggestion", suggestion.DateSuggestion, DbType.DateTime2);
            parameters.Add("@OriginalPlace", suggestion.OriginalPlace, DbType.String);
            parameters.Add("@SuggestedAlternatives", suggestion.SuggestedAlternatives, DbType.String);
            parameters.Add("@Reason", suggestion.Reason, DbType.String);
            parameters.Add("@LocationName", suggestion.LocationName, DbType.String);
            parameters.Add("@EventId", suggestion.EventId, DbType.Int32);
            parameters.Add("@ForecastId", suggestion.ForecastId, DbType.Int32);
            parameters.Add("@TrafficId", suggestion.TrafficId, DbType.Int32);

            await _connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            const string sql = @"SELECT * FROM Suggestion WHERE Active = 1 ORDER BY DateSuggestion DESC;";
            return await _connection.QueryAsync<Suggestion>(sql);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id)
        {
            const string sql = @"SELECT * FROM Suggestion WHERE EventId = @Id AND Active = 1 ORDER BY DateSuggestion DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id)
        {
            const string sql = @"SELECT * FROM Suggestion WHERE ForecastId = @Id AND Active = 1 ORDER BY DateSuggestion DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id)
        {
            const string sql = @"SELECT * FROM Suggestion WHERE TrafficId = @Id AND Active = 1 ORDER BY DateSuggestion DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task DeleteSuggestionAsync(int id)
        {
            const string sql = @"
                            UPDATE Suggestion
                            SET Active = 0,
                                DateDeleted = SYSUTCDATETIME()
                            WHERE Id = @Id;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            var affected = await _connection.ExecuteAsync(sql, parameters);
            if (affected == 0)
                throw new KeyNotFoundException($"Suggestion #{id} non-existent or already deleted.");
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByPlaceCrowdAsync(string placeName)
        {
            const string sql = @"
                            SELECT s.*
                            FROM Suggestion s
                            INNER JOIN CrowdInfo c ON s.OriginalPlace = c.LocationName AND c.Active = 1
                            INNER JOIN Place p ON p.Name = c.LocationName AND p.Active = 1
                            WHERE LOWER(p.Name) = LOWER(@PlaceName)
                              AND s.Active = 1
                            ORDER BY s.DateSuggestion DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@PlaceName", placeName, DbType.String);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetSuggestionsGroupedByPlaceAsync(
            string? typeFilter = null,
            bool? indoorFilter = null,
            DateTime? sinceDate = null)
        {
            const string baseSql = @"
                                SELECT 
                                    s.OriginalPlace                             AS PlaceName,
                                    MAX(p.Type)                                 AS Type,
                                    CAST(MAX(CAST(p.Indoor AS TINYINT)) AS BIT) AS Indoor,
                                    MAX(p.Latitude)                             AS Latitude,
                                    MAX(p.Longitude)                            AS Longitude,
                                    MAX(c.CrowdLevel)                           AS CrowdLevel,
                                    COUNT(*)                                    AS SuggestionCount,
                                    MAX(s.DateSuggestion)                       AS LastSuggestedAt
                                FROM Suggestion s
                                LEFT JOIN Place p ON s.OriginalPlace = p.Name AND p.Active = 1
                                LEFT JOIN CrowdInfo c ON c.LocationName = s.OriginalPlace AND c.Active = 1
                                WHERE s.Active = 1
                                /**WHERE_FILTER**/
                                GROUP BY s.OriginalPlace
                                ORDER BY LastSuggestedAt DESC;";

            var filters = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                filters.Add("p.Type = @TypeFilter");
                parameters.Add("@TypeFilter", typeFilter, DbType.String);
            }

            if (indoorFilter.HasValue)
            {
                filters.Add("p.Indoor = @IndoorFilter");
                parameters.Add("@IndoorFilter", indoorFilter.Value, DbType.Boolean);
            }

            if (sinceDate.HasValue)
            {
                filters.Add("s.DateSuggestion >= @SinceDate");
                parameters.Add("@SinceDate", sinceDate.Value, DbType.DateTime2);
            }

            var whereClause = filters.Count > 0
                ? " AND " + string.Join(" AND ", filters)
                : string.Empty;

            var sql = baseSql.Replace("/**WHERE_FILTER**/", whereClause);

            return await _connection.QueryAsync<SuggestionGroupedByPlaceDTO>(sql, parameters);
        }

        // ======================
        // Interactions GPT
        // ======================

        public async Task SaveInteractionAsync(string prompt, string response, DateTime timestamp)
        {
            var interaction = new GPTInteraction
            {
                Prompt = prompt,
                Response = response,
                CreatedAt = timestamp,
                Active = true
            };

            await SaveInteractionAsync(interaction);
        }

        public async Task<GPTInteraction> CreatePendingAsync(GPTInteraction interaction, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(interaction);

            interaction.Prompt ??= string.Empty;
            interaction.Response ??= string.Empty;
            interaction.Active = true;
            interaction.CreatedAt = interaction.CreatedAt == default
                ? DateTime.UtcNow
                : interaction.CreatedAt;

            interaction.Model ??= "mistral";
            interaction.Temperature ??= 0.3f;
            interaction.SourceType ??= "MistralLocal";

            _logger.LogInformation(
                "Creating pending GPT interaction. PromptLength={PromptLength}, Latitude={Latitude}, Longitude={Longitude}",
                interaction.Prompt.Length,
                interaction.Latitude,
                interaction.Longitude);

            var created = await UpsertInteractionAsync(interaction);

            if (created is null)
                throw new InvalidOperationException("Failed to create pending GPT interaction.");

            return created;
        }

        public async Task SaveInteractionAsync(GPTInteraction interaction)
        {
            ArgumentNullException.ThrowIfNull(interaction);

            var promptHash = ComputePromptHash(interaction.Prompt);

            const string sql = @"
                            MERGE [dbo].[GptInteractions] WITH (HOLDLOCK) AS t
                            USING (SELECT @PromptHash AS PromptHash) AS s
                            ON (t.PromptHash = s.PromptHash)
                            WHEN MATCHED THEN
                                UPDATE SET
                                    Response = @Response,
                                    CreatedAt = SYSUTCDATETIME(),
                                    Active = 1,
                                    Model = @Model,
                                    Temperature = @Temperature,
                                    TokenCount = @TokenCount
                            WHEN NOT MATCHED THEN
                                INSERT (Prompt, PromptHash, Response, CreatedAt, Active, Model, Temperature, TokenCount)
                                VALUES (@Prompt, @PromptHash, @Response, SYSUTCDATETIME(), 1, @Model, @Temperature, @TokenCount);";

            var parameters = new DynamicParameters();
            parameters.Add("@Prompt", interaction.Prompt, DbType.String);
            parameters.Add("@PromptHash", promptHash, DbType.String);
            parameters.Add("@Response", interaction.Response, DbType.String);
            parameters.Add("@Model", interaction.Model ?? "mistral-small-latest", DbType.String);
            parameters.Add("@Temperature", interaction.Temperature ?? 0.7f, DbType.Single);
            parameters.Add("@TokenCount", interaction.TokenCount, DbType.Int32);

            await _connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
        {
            const string sql = @"SELECT * FROM [GptInteractions] WHERE Active = 1 ORDER BY CreatedAt DESC;";
            return await _connection.QueryAsync<GPTInteraction>(sql);
        }

        public async Task<GPTInteraction?> GetByIdAsync(int id)
        {
            const string sql = @"SELECT * FROM [GptInteractions] WHERE Id = @Id AND Active = 1;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            return await _connection.QueryFirstOrDefaultAsync<GPTInteraction>(sql, parameters);
        }

        public async Task<string> AskAsync(string question)
        {
            var simulatedResponse = $"(Simulated GPT Response) You asked : \"{question}\"";

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
                            WHERE Id = @Id
                              AND Active = 1;";

            var parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            var affected = await _connection.ExecuteAsync(sql, parameters);
            return affected > 0;
        }

        public async Task<int> ArchivePastGptInteractionsAsync()
        {
            const string sql = @"
                            UPDATE [GptInteractions]
                            SET Active = 0
                            WHERE Active = 1
                              AND CreatedAt < DATEADD(DAY, -1, SYSUTCDATETIME());";

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
            ArgumentNullException.ThrowIfNull(interaction);

            var promptHash = ComputePromptHash(interaction.Prompt);

            var parameters = new DynamicParameters();
            parameters.Add("@Prompt", interaction.Prompt, DbType.String);
            parameters.Add("@PromptHash", promptHash, DbType.String);
            parameters.Add("@Response", interaction.Response, DbType.String);
            parameters.Add("@Model", interaction.Model, DbType.String);
            parameters.Add("@Temperature", interaction.Temperature, DbType.Single);
            parameters.Add("@TokenCount", interaction.TokenCount, DbType.Int32);

            try
            {
                _logger.LogInformation(
                    "Calling sp_GptInteraction_Upsert. PromptHash={PromptHash}, ResponseLength={ResponseLength}",
                    promptHash,
                    interaction.Response?.Length ?? 0);

                var result = await _connection.QuerySingleOrDefaultAsync<GPTInteraction>(
                    "dbo.sp_GptInteraction_Upsert",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                _logger.LogInformation(
                    "sp_GptInteraction_Upsert completed. PromptHash={PromptHash}, ReturnedId={ReturnedId}, ReturnedResponseLength={ReturnedResponseLength}",
                    promptHash,
                    result?.Id ?? 0,
                    result?.Response?.Length ?? 0);

                if (result is null)
                {
                    _logger.LogError(
                        "sp_GptInteraction_Upsert returned null for prompt: {Prompt}",
                        interaction.Prompt);

                    throw new InvalidOperationException("Failed to upsert GPT interaction.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing sp_GptInteraction_Upsert for prompt: {Prompt}",
                    interaction.Prompt);

                throw;
            }
        }

        public async Task<bool> UpdateResponseAsync(int interactionId, string response, CancellationToken ct = default)
        {
            const string sql = @"
                            UPDATE [GptInteractions]
                            SET Response = @Response,
                                Active = 1
                            WHERE Id = @Id;";

            var affected = await _connection.ExecuteAsync(sql, new
            {
                Id = interactionId,
                Response = response ?? string.Empty
            });

            _logger.LogInformation(
                "UpdateResponseAsync executed. InteractionId={InteractionId}, Updated={Updated}, ResponseLength={ResponseLength}",
                interactionId,
                affected > 0,
                response?.Length ?? 0);

            return affected > 0;
        }

        public async Task<bool> MarkFailedAsync(int interactionId, string? errorMessage, CancellationToken ct = default)
        {
            const string sql = @"
                            UPDATE [GptInteractions]
                            SET Response = COALESCE(NULLIF(Response, ''), @ErrorMessage),
                                Active = 1
                            WHERE Id = @Id;";

            var safeMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? "GPT request failed."
                : $"GPT request failed: {errorMessage}";

            var affected = await _connection.ExecuteAsync(sql, new
            {
                Id = interactionId,
                ErrorMessage = safeMessage
            });

            _logger.LogWarning(
                "MarkFailedAsync executed. InteractionId={InteractionId}, Updated={Updated}, ErrorMessage={ErrorMessage}",
                interactionId,
                affected > 0,
                safeMessage);

            return affected > 0;
        }
    }
}






























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.