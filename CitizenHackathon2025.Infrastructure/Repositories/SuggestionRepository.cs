using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class SuggestionRepository : ISuggestionRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;

        public SuggestionRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }
        public async Task<IEnumerable<Suggestion?>> GetLatestSuggestionAsync()
        {
            try
            {
                const string sql = "SELECT * FROM Suggestion WHERE Active = 1";
                var suggestions = await _connection.QueryAsync<Suggestion?>(sql);
                return suggestions.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving latest suggestions: {ex.Message}");
                return new List<Suggestion>();
            }
        }

        public async Task<IEnumerable<Suggestion?>> GetSuggestionsByUserAsync(int userId)
        {
            try
            {
                const string sql = "SELECT * FROM Suggestion WHERE User_Id = @UserId AND Active = 1";
                var suggestions = await _connection.QueryAsync<Suggestion?>(sql, new { UserId = userId });
                return [.. suggestions];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving suggestions for user {userId}: {ex.Message}");
                return new List<Suggestion>();
            }
        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion)
        {
            try
            {
                const string sql = @"INSERT INTO Suggestion (User_Id, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason)
                     OUTPUT INSERTED.*
                     VALUES (@User_Id, @DateSuggestion, @OriginalPlace, @SuggestedAlternatives, @Reason)";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@User_Id", suggestion.User_Id);
                parameters.Add("@DateSuggestion", suggestion.DateSuggestion);
                parameters.Add("@OriginalPlace", suggestion.OriginalPlace);
                parameters.Add("@SuggestedAlternatives", suggestion.SuggestedAlternatives);
                parameters.Add("@Reason", suggestion.Reason);

                //int rowsAffected = await _connection.ExecuteAsync(sql, parameters);
                //return null;
                var inserted = await _connection.QuerySingleAsync<Suggestion>(sql, parameters);
                return inserted;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error adding Suggestion: {ex}");
                return null;
            }
        }
        public async Task<Suggestion?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = "SELECT * FROM Suggestion WHERE Id = @Id AND Active = 1";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id, DbType.Int64);

                var suggestion = await _connection.QueryFirstOrDefaultAsync<Suggestion?>(sql, parameters);

                return suggestion;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error geting Suggestion : {ex.ToString}");
                return null;
            }

        }

        public Suggestion UpdateSuggestion(Suggestion suggestion)
        {
            if (suggestion == null || suggestion.Id <= 0)
            {
                throw new ArgumentException("Invalid suggestion to update.", nameof(suggestion));
            }
            try
            {
                string sql = "UPDATE Suggestion SET User_Id = @UserId, DateSuggestion = @DateSuggestion, OriginalPlace = @OriginalPlace, SuggestedAlternatives = @SuggestedAlternatives, Reason = @Reason WHERE Id = @Id AND Active = 1";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", suggestion.Id);
                parameters.Add("@UserId", suggestion.User_Id);
                parameters.Add("@DateSuggestion", suggestion.DateSuggestion);
                parameters.Add("@OriginalPlace", suggestion.OriginalPlace);
                parameters.Add("@SuggestedAlternatives", suggestion.SuggestedAlternatives);
                parameters.Add("@Reason", suggestion.Reason);

                var affectedRows = _connection.Execute(sql, parameters);

                return affectedRows > 0 ? suggestion : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating suggestion: {ex.Message}");
            }
            return null;
        }
        public async Task<bool> SoftDeleteSuggestionAsync(int id)
        {
            try
            {
                const string sql = @"
                    UPDATE Suggestion
                    SET Active = 0,
                        DateDeleted = GETDATE()
                    WHERE Id = @Id AND Active = 1";

                int rows = await _connection.ExecuteAsync(sql, new { Id = id });
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during soft delete: {ex.Message}");
                return false;
            }
        }
    }
}























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.