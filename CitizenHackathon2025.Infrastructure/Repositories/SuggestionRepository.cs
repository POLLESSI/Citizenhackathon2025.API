using System;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

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
                string sql = " SELECT * FROM Suggestion WHERE Active = 1";

                var suggestions = await _connection.QueryAsync<Suggestion?>(sql);
                return [.. suggestions];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Suggestion: {ex.Message}");
                return [];
            }

        }

        public async Task<Suggestion> SaveSuggestionAsync(Suggestion suggestion)
        {
            try
            {
                const string sql = "INSERT INTO Suggestion (UserId, DateSuggestion, OriginalPlace, SuggestedAlternatives, Reason)" +
                "VALUES (@UserId, @DateSuggestion, @OriginalPlace, @SuggestedAlternative, @Reason)";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@UserId", suggestion.UserId);
                parameters.Add("@DateSuggestion", suggestion.DateSuggestion);
                parameters.Add("@OriginalPlace", suggestion.OriginalPlace);
                parameters.Add("@SuggestedAlternatives", suggestion.SuggestedAlternatives);
                parameters.Add("@Reason", suggestion.Reason);

                int rowsAffected = await _connection.ExecuteAsync(sql, parameters);
                return null;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error adding Suggestion: {ex.ToString()}");
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
                parameters.Add("@UserId", suggestion.UserId);
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
    }
}
