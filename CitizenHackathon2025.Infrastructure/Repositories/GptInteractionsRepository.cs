using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Entities;
using Dapper;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class GptInteractionsRepository : IGPTRepository
    {
#nullable disable
        private readonly IDbConnection _connection;
        /* private readonly string _apiKey = "sk-..."; */ // to inject via IConfiguration if you use it
        // private readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
        // private readonly string _model = "gpt-4o";
        public GptInteractionsRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }
        public async Task SaveSuggestionAsync(Suggestion suggestion)
        {
            const string sql = @"
                        INSERT INTO Suggestion (User_Id, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason, Active, DateDeleted, EventId, ForecastId, TrafficId, LocationName)
                        VALUES (@User_Id, @DateSuggestion, @OriginalPlace, @SuggestedAlternatives, @Reason, @Active, @DateDeleted, @EventId, @ForecastId, @TrafficId, @LocationName);
                        SELECT CAST(SCOPE_IDENTITY() as int);";

            try
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@User_Id", suggestion.User_Id, DbType.Int32);
                parameters.Add("@DateSuggestion", suggestion.DateSuggestion, DbType.DateTime);
                parameters.Add("@OriginalPlace", suggestion.OriginalPlace, DbType.String);
                parameters.Add("@SuggestedAlternatives", suggestion.SuggestedAlternatives, DbType.String);
                parameters.Add("@Reason", suggestion.Reason, DbType.String);

                // Execute the query and retrieve the generated Id
                int newId = await _connection.QuerySingleAsync<int>(sql, parameters);
                suggestion.Id = newId;
            }
            catch (Exception ex)
            {
                // Log or error management
                throw new DataException("Error saving GPT suggestion", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }
        public Task SaveInteractionAsync(GPTInteraction interaction)
        {
            // Hash SHA-256 hex (64 chars) du prompt pour l’UPSERT idempotent
            var promptHash = Sha256Hex(interaction.Prompt ?? string.Empty);

            const string sql = @"
                            MERGE [GptInteractions] AS t
                            USING (SELECT @PromptHash AS PromptHash) AS s
                            ON (t.PromptHash = s.PromptHash)
                            WHEN MATCHED THEN
                                UPDATE SET
                                    Response  = @Response,
                                    CreatedAt = SYSUTCDATETIME(),
                                    Active    = 1
                            WHEN NOT MATCHED THEN
                                INSERT (Prompt, PromptHash, Response, Active)
                                VALUES (@Prompt, @PromptHash, @Response, 1);";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Prompt", interaction.Prompt, DbType.String);
            parameters.Add("PromptHash", promptHash, DbType.String);
            parameters.Add("Response", interaction.Response, DbType.String);
            static string Sha256Hex(string input)
            {
                using var sha256 = SHA256.Create();
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
            return _connection.ExecuteAsync(sql, parameters);
        }
        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            var sql = "SELECT * FROM Suggestion WHERE Active = 1 ORDER BY DateSuggestion DESC";
            return await _connection.QueryAsync<Suggestion>(sql);
        }
        public Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
            => _connection.QueryAsync<GPTInteraction>("SELECT * FROM [GptInteractions] WHERE Active = 1 ORDER BY CreatedAt DESC");


        public async Task<GPTInteraction?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM [GptInteractions] WHERE Id = @Id AND Active = 1";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryFirstOrDefaultAsync<GPTInteraction>(sql, parameters);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id)
        {
            const string sql = @"
                SELECT * FROM Suggestion
                WHERE EventId = @Id
                AND Active = 1
                ORDER BY DateSuggestion DESC";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id)
        {
            const string sql = @"
                SELECT * FROM Suggestion
                WHERE ForecastId = @Id
                AND Active = 1
                ORDER BY DateSuggestion DESC";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id)
        {
            const string sql = @"
                SELECT * FROM Suggestion
                WHERE TrafficId = @Id
                AND Active = 1
                ORDER BY DateSuggestion DESC";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("TrafficId", id, DbType.Int32);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task DeleteSuggestionAsync(int id)
        {
            const string sql = "UPDATE Suggestion SET Active = 0, DateDeleted = GETDATE() WHERE Id = @Id";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);

            int rowsAffected = await _connection.ExecuteAsync(sql, parameters);
            // If no rows were affected, it means the suggestion does not exist or is already deleted
            if (rowsAffected == 0)
            {
                throw new KeyNotFoundException($"Suggestion with ID {id} does not exist or is already deleted.");
            }
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByPlaceCrowdAsync(string placeName)
        {
            const string sql = @"
                SELECT s.*
                FROM Suggestion s
                INNER JOIN CrowdInfo c ON s.OriginalPlace = c.LocationName
                INNER JOIN Place p ON p.Name = c.LocationName
                WHERE LOWER(p.Name) = LOWER(@PlaceName)
                  AND s.Active = 1
                  AND c.Active = 1
                  AND p.Active = 1
                ORDER BY s.DateSuggestion DESC";

            var parameters = new DynamicParameters();
            parameters.Add("@PlaceName", placeName, DbType.String);

            return await _connection.QueryAsync<Suggestion>(sql, parameters);
        }

        public async Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetSuggestionsGroupedByPlaceAsync(string? typeFilter = null, bool? indoorFilter = null, DateTime? sinceDate = null)
        {
            var sql = @"
                    SELECT 
                        s.OriginalPlace AS PlaceName,
                        MAX(p.Type) AS Type,
                        MAX(p.Indoor) AS Indoor,
                        MAX(p.Latitude) AS Latitude,
                        MAX(p.Longitude) AS Longitude,
                        MAX(c.CrowdLevel) AS CrowdLevel,
                        COUNT(*) AS SuggestionCount,
                        MAX(s.DateSuggestion) AS LastSuggestedAt
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
                parameters.Add("TypeFilter", typeFilter);
            }

            if (indoorFilter.HasValue)
            {
                filters.Add("p.Indoor = @IndoorFilter");
                parameters.Add("IndoorFilter", indoorFilter.Value);
            }

            if (sinceDate.HasValue)
            {
                filters.Add("s.DateSuggestion >= @SinceDate");
                parameters.Add("SinceDate", sinceDate.Value);
            }

            if (filters.Any())
            {
                sql = sql.Replace("/**WHERE_FILTER**/", "AND " + string.Join(" AND ", filters));
            }
            else
            {
                sql = sql.Replace("/**WHERE_FILTER**/", "");
            }

            var grouped = (await _connection.QueryAsync<SuggestionGroupedByPlaceDTO>(sql, parameters)).ToList();

            const string suggestionSql = @"
                    SELECT * FROM Suggestion
                    WHERE Active = 1 AND OriginalPlace = @PlaceName
                    ORDER BY DateSuggestion DESC";
            DynamicParameters suggestionParameters = new DynamicParameters();

            foreach (var group in grouped)
            {
                suggestionParameters = new DynamicParameters();
                suggestionParameters.Add("PlaceName", group.PlaceName);
                var suggestions = await _connection.QueryAsync<Suggestion>(suggestionSql, suggestionParameters);
                group.Suggestions = suggestions.ToList();
            }

            return grouped;
        }

        //Simulated response
        public async Task<string> AskAsync(string question)
        {
            // Response simulation
            string simulatedResponse = $"(Simulated GPT Response) You asked : \"{question}\"";

            var interaction = new GPTInteraction
            {
                Prompt = question,
                Response = simulatedResponse,
                CreatedAt = DateTime.UtcNow
            };

            await SaveInteractionAsync(interaction);
            return simulatedResponse;
        }

        public Task<bool> DeactivateInteractionAsync(int id)
                    => _connection.ExecuteAsync(@"
                                    UPDATE [GptInteractions]
                                    SET Active = 0
                                    WHERE Id = @Id AND Active = 1;", new
                    {
                        Id = id
                    })
                .ContinueWith(t => t.Result > 0);

        // To be used when we have real AI behind it
        //public async Task<string> AskAsync(string question)
        //{
        //    using var client = new HttpClient();
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        //    var requestBody = new
        //    {
        //        model = _model,
        //        messages = new[]
        //        {
        //        new { role = "system", content = "You are an assistant for a citizen smart city project." },
        //        new { role = "user", content = question }
        //    },
        //        temperature = 0.5
        //    };

        //    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        //    using var response = await client.PostAsync(_endpoint, content);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        var error = await response.Content.ReadAsStringAsync();
        //        throw new Exception($"GPT request failed: {error}");
        //    }

        // ======================
        // Helpers
        // ======================
        private static string Sha256Hex(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            return Convert.ToHexString(bytes); // 64 hex uppercase
        }
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.