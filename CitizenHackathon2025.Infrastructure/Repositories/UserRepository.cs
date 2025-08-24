using Citizenhackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;
using CitizenHackathon2025.Shared.StaticConfig.Constants;
using CitizenHackathon2025.Shared.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac.Model;
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
        private readonly IDbConnection _connection;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IDbConnection connection, ILogger<UserRepository> logger)
        {
            _connection = connection;
            _logger = logger;

            // Registers the TypeHandler for UserRole
            SqlMapper.AddTypeHandler(new RoleTypeHandler());
        }

        public async Task<Users> RegisterUserAsync(string email, byte[] passwordHash, Users user)
        {
            try
            {
                var sql = @"
                INSERT INTO [Users] 
                    (Email, PasswordHash, SecurityStamp, Role, Status, Active)
                VALUES 
                    (@Email, @PasswordHash, @SecurityStamp, @Role, @Status, @Active);";

                var parameters = new DynamicParameters();
                parameters.Add("Email", user.Email, DbType.String);
                parameters.Add("PasswordHash", passwordHash, DbType.Binary);
                parameters.Add("SecurityStamp", user.SecurityStamp, DbType.Guid);
                parameters.Add("Role", (int)user.Role, DbType.Int32); // ✅ conversion int
                parameters.Add("Status", (int)user.Status, DbType.Int32);
                parameters.Add("Active", user.Active, DbType.Boolean);

                await _connection.ExecuteAsync(sql, parameters);

                return user;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while registering user {Email}", user.Email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering user {Email}", user.Email);
                throw;
            }
        }

        public async Task<Users> GetUserByEmailAsync(string email)
        {
            var sql = "SELECT * FROM [Users] WHERE Email = @Email AND Active = 1";
            return await _connection.QueryFirstOrDefaultAsync<Users>(sql, new { Email = email });
        }

        public async Task<Users> GetUserByIdAsync(int id)
        {
            var sql = "SELECT * FROM [Users] WHERE Id = @Id";
            return await _connection.QueryFirstOrDefaultAsync<Users>(sql, new { Id = id });
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !user.Active || user.Status != (int)UserStatus.Active)
                return false;

            var hash = HashHelper.HashPassword(password, user.SecurityStamp.ToString());
            return StructuralComparisons.StructuralEqualityComparer.Equals(hash, user.PasswordHash);
        }

        public Task<Users> LoginUsingProcedureAsync(string email, string password)
        {
            throw new NotImplementedException();
        }

        public void SetRole(int id, string role)
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
                throw new ArgumentException("Invalid role format", nameof(role));

            var sql = "UPDATE [Users] SET Role = @Role WHERE Id = @Id";
            _connection.Execute(sql, new { Id = id, Role = (int)parsedRole });
        }

        public async Task DeactivateUserAsync(int id)
        {
            var sql = "UPDATE [Users] SET Active = 0 WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, new { Id = id });
        }

        public Users UpdateUser(Users user)
        {
            var sql = @"
            UPDATE [Users]
            SET Email = @Email, Role = @Role, Status = @Status, Active = @Active
            WHERE Id = @Id";
            _connection.Execute(sql, new
            {
                user.Id,
                Role = (int)user.Role, // ✅ int conversion
                Status = (int)user.Status,
                user.Email,
                user.Active
            });

            return user;
        }

        public async Task<IEnumerable<Users>> GetAllActiveUsersAsync()
        {
            var sql = "SELECT * FROM [Users] WHERE Active = 1";
            return await _connection.QueryAsync<Users>(sql);
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.