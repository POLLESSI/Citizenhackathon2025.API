using Azure;
using Citizenhackathon2025.Domain.Entities;
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
    public class GptInteractionsRepository : IGptInteractionRepository
    {
#nullable disable
        private readonly System.Data.IDbConnection _connection;

        public GptInteractionsRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }
        public async Task SaveInteractionAsync(string prompt, string gptResponse, DateTime timestamp)
        {
            string sql = "INSERT INTO GptInteractions (Id, Prompt, Response) VALUES (@Id, @Prompt, @Response)";

            try
            {
                await _connection.ExecuteAsync(sql, new
                {
                    Id = Guid.NewGuid(),
                    Prompt = prompt,
                    Response = gptResponse,
                    Timestamp = timestamp
                });
            }
            catch (SqlException ex) when (ex.Number == 2627) // violation de contrainte UNIQUE
            {

                var update = @"UPDATE GptInteractions SET Response = @Response WHERE Prompt = @Prompt AND Active = 1";
                await _connection.ExecuteAsync(update, new { Prompt = prompt, Response = gptResponse });
            }
        }
            public async Task<IEnumerable<GPTInteraction>> GetAllInteractionsAsync()
        {
            var sql = "SELECT * FROM GptInteractions WHERE Active = 1 ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<GPTInteraction>(sql);
        }
    }
}
