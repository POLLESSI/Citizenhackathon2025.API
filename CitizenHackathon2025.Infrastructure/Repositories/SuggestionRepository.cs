using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class SuggestionRepository : ISuggestionRepository
    {
    #nullable disable
        private readonly IDbConnection _connection;
        private readonly ILogger<SuggestionRepository> _logger;

        public SuggestionRepository(IDbConnection connection, ILogger<SuggestionRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<Suggestion?>> GetAllSuggestionsAsync()
        {
            const string sql = @"
                            SELECT *
                            FROM dbo.Suggestion
                            WHERE Active = 1
                            ORDER BY DateSuggestion DESC;";
            try
            {
                return await _connection.QueryAsync<Suggestion?>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all suggestions");
                return Enumerable.Empty<Suggestion>();
            }
        }

        public async Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync()
        {
            const string sql = @"
                            SELECT TOP (100) *
                            FROM dbo.Suggestion
                            WHERE Active = 1
                            ORDER BY DateSuggestion DESC";
            return await _connection.QueryAsync<Suggestion?>(sql);
        }

        public async Task<Suggestion?> GetByIdAsync(int id)
        {
            const string sql = @"
                            SELECT Id, User_Id, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason,
                                   Active, DateDeleted, EventId, ForecastId, TrafficId, LocationName
                            FROM dbo.Suggestion
                            WHERE Id = @Id AND Active = 1;";
            DynamicParameters parameters = new();
            parameters.Add("Id", id, DbType.Int32);

            try
            {
                return await _connection.QueryFirstOrDefaultAsync<Suggestion>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Suggestion by Id={Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<Suggestion?>> GetSuggestionsByUserAsync(int userId)
        {
            const string sql = @"
                            SELECT *
                            FROM dbo.Suggestion
                            WHERE User_Id = @UserId AND Active = 1
                            ORDER BY DateSuggestion DESC;";
            DynamicParameters parameters = new();
            parameters.Add("UserId", userId, DbType.Int32);

            try
            {
                return await _connection.QueryAsync<Suggestion?>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suggestions for user {UserId}", userId);
                return Enumerable.Empty<Suggestion>();
            }
        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion)
        {
            const string sql = @"
                            INSERT INTO dbo.Suggestion
                            (User_Id, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason, EventId, ForecastId, TrafficId, LocationName)
                            OUTPUT INSERTED.*
                            VALUES (@User_Id, @DateSuggestion, @OriginalPlace, @SuggestedAlternatives, @Reason, @EventId, @ForecastId, @TrafficId, @LocationName);";
            DynamicParameters parameters = new();
            parameters.Add("User_Id", suggestion.User_Id, DbType.Int32);
            parameters.Add("DateSuggestion", suggestion.DateSuggestion, DbType.DateTime2);
            parameters.Add("OriginalPlace", suggestion.OriginalPlace, DbType.String);
            parameters.Add("SuggestedAlternatives", suggestion.SuggestedAlternatives, DbType.String);
            parameters.Add("Reason", suggestion.Reason, DbType.String);
            parameters.Add("EventId", suggestion.EventId, DbType.Int32);
            parameters.Add("ForecastId", suggestion.ForecastId, DbType.Int32);
            parameters.Add("TrafficId", suggestion.TrafficId, DbType.Int32);
            parameters.Add("LocationName", suggestion.LocationName, DbType.String);

            try
            {
                return await _connection.QuerySingleAsync<Suggestion>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Suggestion");
                return null!;
            }
        }

        public Suggestion? UpdateSuggestion(Suggestion suggestion)
        {
            if (suggestion is null || suggestion.Id <= 0)
                throw new ArgumentException("Invalid suggestion to update.", nameof(suggestion));

            const string sql = @"
                            UPDATE dbo.Suggestion
                            SET User_Id = @User_Id,
                                DateSuggestion = @DateSuggestion,
                                OriginalPlace = @OriginalPlace,
                                SuggestedAlternatives = @SuggestedAlternatives,
                                Reason = @Reason,
                                EventId = @EventId,
                                ForecastId = @ForecastId,
                                TrafficId = @TrafficId,
                                LocationName = @LocationName
                            WHERE Id = @Id AND Active = 1;";
            DynamicParameters parameters = new();
            parameters.Add("Id", suggestion.Id, DbType.Int32);
            parameters.Add("User_Id", suggestion.User_Id, DbType.Int32);
            parameters.Add("DateSuggestion", suggestion.DateSuggestion, DbType.DateTime2);
            parameters.Add("OriginalPlace", suggestion.OriginalPlace, DbType.String);
            parameters.Add("SuggestedAlternatives", suggestion.SuggestedAlternatives, DbType.String);
            parameters.Add("Reason", suggestion.Reason, DbType.String);
            parameters.Add("EventId", suggestion.EventId, DbType.Int32);
            parameters.Add("ForecastId", suggestion.ForecastId, DbType.Int32);
            parameters.Add("TrafficId", suggestion.TrafficId, DbType.Int32);
            parameters.Add("LocationName", suggestion.LocationName, DbType.String);


            try
            {
                var rows = _connection.Execute(sql, parameters);

                return rows > 0 ? suggestion : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating suggestion Id={Id}", suggestion.Id);
                return null;
            }
        }

        public async Task<bool> SoftDeleteSuggestionAsync(int id)
        {
            const string sql = @"
                            UPDATE dbo.Suggestion
                            SET Active = 0, DateDeleted = SYSUTCDATETIME()
                            WHERE Id = @Id AND Active = 1;";
            DynamicParameters parameters = new();
            parameters.Add("Id", id, DbType.Int32);

            try
            {
                var rows = await _connection.ExecuteAsync(sql, parameters);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during soft delete Id={Id}", id);
                return false;
            }
        }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.