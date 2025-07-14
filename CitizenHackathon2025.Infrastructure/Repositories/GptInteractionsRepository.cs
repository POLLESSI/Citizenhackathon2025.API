using Azure;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.DTOs;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Interfaces;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class GptInteractionsRepository : IGPTRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;
       /* private readonly string _apiKey = "sk-..."; */// to be injected into a real project via IConfiguration
        private readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
        private readonly string _model = "gpt-4";
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

                var parameters = new
                {
                    suggestion.User_Id,
                    suggestion.DateSuggestion,
                    suggestion.OriginalPlace,
                    suggestion.SuggestedAlternatives,
                    suggestion.Reason,
                    suggestion.Active,
                    suggestion.DateDeleted,
                    suggestion.EventId,
                    suggestion.ForecastId,
                    suggestion.TrafficId,
                    suggestion.LocationName
                };

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
        public async Task SaveInteractionAsync(GPTInteraction interaction)
        {
            string sql = "INSERT INTO GptInteractions (Id, Prompt, Response, CreatedAt, Active) VALUES (@Id, @Prompt, @Response, @CreatedAt, 1)";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", Guid.NewGuid(), DbType.Guid);
            parameters.Add("@Prompt", interaction.Prompt);
            parameters.Add("@Response", interaction.Response);
            parameters.Add("@CreatedAt", interaction.CreatedAt);

            try
            {
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (SqlException ex) when (ex.Number == 2627) // UNIQUE constraint violation
            {

                var update = @"UPDATE GptInteractions SET Response = @Response WHERE Prompt = @Prompt AND Active = 1";
                DynamicParameters updateParams = new DynamicParameters();
                updateParams.Add("@Prompt", interaction.Prompt);
                updateParams.Add("@Response", interaction.Response);
                // If the prompt already exists, update the response instead of inserting a new record
                await _connection.ExecuteAsync(update, updateParams);
            }
        }
        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            var sql = "SELECT * FROM Suggestion WHERE Active = 1 ORDER BY DateSuggestion DESC";
            return await _connection.QueryAsync<Suggestion>(sql);
        }
        public async Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
        {
            var sql = "SELECT * FROM GptInteractions WHERE Active = 1 ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<GPTInteraction>(sql);
        }

        public async Task<GPTInteraction?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM GPTInteractions WHERE Id = @Id AND Active = 1";
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

        public async Task<IEnumerable<SuggestionGroupedByPlaceDTO>> GetSuggestionsGroupedByPlaceAsync( string? typeFilter = null, bool? indoorFilter = null, DateTime? sinceDate = null)
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

        public async Task<bool> DeactivateInteractionAsync(int id)
        {
            const string sql = @"
                        UPDATE GPTInteraction
                        SET Active = 0,
                            DeletedAt = GETDATE()
                        WHERE Id = @Id AND Active = 1";

            try
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                var affectedRows = await _connection.ExecuteAsync(sql, new { Id = id });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                throw new DataException($"Error disabling GPT interaction (id: {id})", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

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

        //    var json = await response.Content.ReadAsStringAsync();
        //    using var doc = JsonDocument.Parse(json);
        //    var answer = doc.RootElement
        //        .GetProperty("choices")[0]
        //        .GetProperty("message")
        //        .GetProperty("content")
        //        .GetString();

        //    // Saving the interaction in the database
        //    var interaction = new GPTInteraction
        //    {
        //        Prompt = question,
        //        Response = answer,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    await SaveInteractionAsync(interaction);
        //    return answer;
        //}
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.