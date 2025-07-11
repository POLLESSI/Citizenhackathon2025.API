using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Data.Common;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class GPTRepository : IGPTRepository
    {
        private readonly System.Data.IDbConnection _connection;
        private readonly ILogger<IGPTRepository> _logger;

        public GPTRepository(System.Data.IDbConnection connection, ILogger<IGPTRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task DeleteSuggestionAsync(int id)
        {
            const string sql = "DELETE FROM Suggestions WHERE Id = @Id";

            try
            {
                await _connection.ExecuteAsync(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting suggestion #{id}");
                throw;
            }
        }

        public async Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            const string sql = "SELECT * FROM Suggestions ORDER BY CreatedAt DESC";

            try
            {
                var suggestions = await _connection.QueryAsync<Suggestion>(sql);
                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all suggestions.");
                return Array.Empty<Suggestion>();
            }
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id)
        {
            return await GetSuggestionsByAsync("EventId", id);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id)
        {
            return await GetSuggestionsByAsync("ForecastId", id);
        }

        public async Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id)
        {
            return await GetSuggestionsByAsync("TrafficConditionId", id);
        }
        /// <summary>
        /// Factored method to retrieve suggestions based on a foreign key criterion.
        /// </summary>
        private async Task<IEnumerable<Suggestion>> GetSuggestionsByAsync(string columnName, int id)
        {
            // Vérification de sécurité minimale (whitelist)
            var allowedColumns = new[] { "EventId", "ForecastId", "TrafficConditionId" };
            if (!allowedColumns.Contains(columnName))
                throw new ArgumentException("Invalid column name", nameof(columnName));

            string sql = $"SELECT * FROM Suggestions WHERE {columnName} = @Id";
            return await _connection.QueryAsync<Suggestion>(sql, new { Id = id });
        }

        public async Task SaveSuggestionAsync(Suggestion suggestion)
        {
            const string sql = @"
                INSERT INTO Suggestions (UserId, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason)
                VALUES (@UserId, @DateSuggestion, @OriginalPlace, @SuggestedAlternatives, @Reason);";

            try
            {
                await _connection.ExecuteAsync(sql, new
                {
                    suggestion.User_Id,
                    suggestion.DateSuggestion,
                    suggestion.OriginalPlace,
                    suggestion.SuggestedAlternatives,
                    suggestion.Reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting a suggestion.");
                throw;
            }
        }
        public async Task SaveInteractionAsync(GPTInteraction interaction)
        {
            var sql = @"
            INSERT INTO GptInteractions (Prompt, Response, CreatedAt, Active)
            VALUES (@Prompt, @Response, @CreatedAt, @Active);
        ";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Prompt", interaction.Prompt);
            parameters.Add("Response", interaction.Response);
            parameters.Add("CreatedAt", interaction.CreatedAt);
            parameters.Add("Active", interaction.Active);

            await _connection.ExecuteAsync(sql, parameters);
        }
        public async Task<IEnumerable<GPTInteraction>> GetAllActiveAsync()
        {
            var sql = "SELECT * FROM GptInteractions WHERE Active = 1 ORDER BY CreatedAt DESC;";
            return await _connection.QueryAsync<GPTInteraction>(sql);
        }

        public async Task SoftDeleteAsync(int id)
        {
            var sql = "DELETE FROM GptInteractions WHERE Id = @Id;";
            await _connection.ExecuteAsync(sql, new { Id = id });
        }
        public Task<string> AskAsync(string question)
        {
            return Task.FromResult("Simulated GPT response");
        }

        public Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<GPTInteraction?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}





































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.