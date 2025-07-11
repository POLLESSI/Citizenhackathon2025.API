using Azure;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class GptInteractionsRepository : IGPTRepository
    {
#nullable disable
        private readonly System.Data.IDbConnection _connection;

        public GptInteractionsRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }
        public Task SaveSuggestionAsync(Suggestion suggestion)
        {
            throw new NotImplementedException();
        }
        public Task SaveInteractionAsync(GPTInteraction interaction)
        {
            throw new NotImplementedException();
        }
        public async Task SaveInteractionAsync(string prompt, string gptResponse, DateTime timestamp)
        {
            string sql = "INSERT INTO GptInteractions (Id, Prompt, Response, Timestamp, Active) VALUES (@Id, @Prompt, @Response, @Timestamp, 1)";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", Guid.NewGuid(), DbType.Guid);
            parameters.Add("Prompt", prompt, DbType.String);
            parameters.Add("Response", gptResponse, DbType.String);
            parameters.Add("Timestamp", timestamp, DbType.DateTime);
            try
            {
                await _connection.ExecuteAsync(sql, parameters);
            }
            catch (SqlException ex) when (ex.Number == 2627) // UNIQUE constraint violation
            {

                var update = @"UPDATE GptInteractions SET Response = @Response WHERE Prompt = @Prompt AND Active = 1";
                DynamicParameters updateParams = new DynamicParameters();
                updateParams.Add("Prompt", prompt, DbType.String);
                updateParams.Add("Response", gptResponse, DbType.String);
                // If the prompt already exists, update the response instead of inserting a new record
                await _connection.ExecuteAsync(update, updateParams);
            }
        }

        public Task<IEnumerable<Suggestion>> GetAllSuggestionsAsync()
        {
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
        {
            var sql = "SELECT * FROM GptInteractions WHERE Active = 1 ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<GPTInteraction>(sql);
        }

        public async Task<GPTInteraction?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM GPTInteractions WHERE Id = @Id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryFirstOrDefaultAsync<GPTInteraction>(sql, parameters);
        }

        public Task<IEnumerable<Suggestion>> GetSuggestionsByEventIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Suggestion>> GetSuggestionsByForecastIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Suggestion>> GetSuggestionsByTrafficIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSuggestionAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<string> AskAsync(string question)
        {
            throw new NotImplementedException();
        }

        
    }
}































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.