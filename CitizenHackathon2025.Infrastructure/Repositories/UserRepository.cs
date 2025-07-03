using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Enums;
using Citizenhackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using CitizenHackathon2025.Shared.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IDbConnection connection, ILogger<UserRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task DeactivateUserAsync(int id)
        {
            var sql = "UPDATE [User] SET Active = 0 WHERE Id = @Id";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);
            await _connection.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
        {
            try
            {
                var sql = "SELECT * FROM [User] WHERE Active = 1";
                // No parameters needed for this query
                return await _connection.QueryAsync<User>(sql);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            try
            {
                var sql = "SELECT * FROM [User] WHERE Email = @Email AND Active = 1";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Email", email, DbType.String);

                return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error email unexitant: {ex.Message}");
            }
            return null;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                var sql = "SELECT * FROM [User] WHERE Id = @Id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Id", id, DbType.Int32);

                return await _connection.QueryFirstOrDefaultAsync<User>(sql, parameters);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Eror Id User unexistant : {ex.Message}");
            }
            return null;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null) return false;

                var hash = HashHelper.HashPassword(password, user.SecurityStamp.ToString());
                return StructuralComparisons.StructuralEqualityComparer.Equals(hash, user.PasswordHash);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error login user : {ex.Message}");
            }
            return false;
        }

        public async Task<User> LoginUsingProcedureAsync(string email, string password)
        {
            try
            {
                var sql = "sqlUserLogin";
                return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { email, password }, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error login with procedure : {ex.Message}");
            }
            return null;
        }

        public async Task<User> RegisterUserAsync(string email, byte[] passwordHash, User user)
        {
            try
            {
                var sql = @"INSERT INTO [User] (Email, PasswordHash, SecurityStamp, Role, Status, Active)
                        VALUES (@Email, @PasswordHash, @SecurityStamp, @Role, @Status, @Active)";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Email", user.Email, DbType.String);
                parameters.Add("PasswordHash", passwordHash, DbType.Binary);
                parameters.Add("SecurityStamp", user.SecurityStamp, DbType.Guid);
                parameters.Add("Role", user.Role.ToString(), DbType.String);
                parameters.Add("Status", (int)user.Status, DbType.Int32);
                parameters.Add("Active", user.Active, DbType.Boolean);

                await _connection.ExecuteAsync(sql, new
                {
                    user.Email,
                    PasswordHash = passwordHash,
                    user.SecurityStamp,
                    Role = user.Role.ToString(),
                    Status = (int)user.Status,
                    user.Active
                });
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering user: {ex.Message}");
            }
            return null;
        }

        public void SetRole(int id, string role)
        {
            try
            {
                var sql = "UPDATE [User] SET Role = @Role WHERE Id = @Id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Id", id, DbType.Int32);
                parameters.Add("Role", role, DbType.String);

                _connection.Execute(sql, parameters);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error changing role: {ex.Message}");
            }
        }

        public User UpdateUser(User user)
        {
            try
            {
                var sql = @"UPDATE [User] SET Email = @Email, Role = @Role, Status = @Status, Active = @Active
                        WHERE Id = @Id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Email", user.Email, DbType.String);
                parameters.Add("Role", user.Role.ToString(), DbType.String);
                parameters.Add("Status", (int)user.Status, DbType.Int32);
                parameters.Add("Active", user.Active, DbType.Boolean);
                _connection.Execute(sql, parameters);

                return user;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error Upgrading User: {ex.Message}");
            }
            return null;
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.