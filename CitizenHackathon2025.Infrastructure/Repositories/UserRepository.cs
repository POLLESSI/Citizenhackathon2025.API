using AutoMapper;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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
        private readonly IMapper _mapper;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IDbConnection connection, IMapper mapper, ILogger<UserRepository> logger)
        {
            _connection = connection;
            _mapper = mapper;
            _logger = logger;
        }

        public Task DeactivateUserAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
        {
            string sql = "SELECT * FROM [User] WHERE Active = 1";
            return _connection.Query<User?>(sql);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                const string sql = "SELECT * FROM [User] WHERE Email = @Email AND Active = 1";
                var parameters = new DynamicParameters();
                parameters.Add("@Email", email);

                return await _connection.QueryFirstOrDefaultAsync<User>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user by email: {email}");
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                string sql = "SELECT * FROM [User] WHERE Id = @id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", id);
                return _connection.QueryFirst<User?>(sql, parameters);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error geting User : {ex.ToString}");
            }
            return null;
        }

        // Login
        Task<bool> IUserRepository.LoginAsync(string email, string password)
        {
            throw new NotImplementedException();
        }
        public async Task<UserDTO?> LoginAsync(string email, string password)
        {
            try
            {
                const string sql = @"
            SELECT Id, Email, Role, Active
            FROM [User]
            WHERE Email = @Email
              AND Active = 1
              AND PasswordHash = dbo.fHasher(@Password, SecurityStamp);";

                var user = await _connection.QueryFirstOrDefaultAsync<User>(
                    sql,
                    new
                    {
                        Email = email,
                        Password = password
                    });

                return user != null ? _mapper.Map<UserDTO>(user) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login failed: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> LoginUsingProcedureAsync(string email, string password)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Email", email);
            parameters.Add("@Password", password); // La proc SQL s’occupera du hash

            var result = await _connection.QueryFirstOrDefaultAsync<User>(
                "sqlUserLogin",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result != null;
        }

        //Register
        public async Task<bool> RegisterUserAsync(string email, byte[] passwordHash, User user)
        {
            try
            {
                string sql = "INSERT INTO [User] (Email, PasswordHash, Role, Active, SecurityStamp) " +
                "VALUES (@email, @passwordHash, @role, 1, NEWID())";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@email", email);
                parameters.Add("@passwordHash", passwordHash, DbType.Binary, size: 64);
                parameters.Add("@role", user.Role);

                // Fix: Use ExecuteAsync instead of Execute for async operations
                int affectedRows = await _connection.ExecuteAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Registrating New User : {ex.ToString()}");
                return false;
            }
        }

        // Patch
        public void SetRole(int id, string role)
        {
            try
            {
                string sql = "UPDATE [User] SET Role = @role WHERE Id = @id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@role", role);
                _connection.Execute(sql, parameters);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error changing rôle : {ex.ToString}");
            }
        }

        // Update
        public User UpdateUser(User user)
        {
            if (user == null || user.Id <= 0)
            {
                throw new ArgumentException("Invalid user for update.", nameof(user));
            }
            try
            {
                string sql = "UPDATE [User] SET Email = @Email, Role = @Role WHERE Id = @Id AND Active = 1"; 
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Id", user.Id);
                parameters.Add("@Email", user.Email);
                parameters.Add("@Role", user.Role);

                var affectedRows = _connection.Execute(sql, parameters);
                return affectedRows > 0 ? user : null;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error updating user with ID {UserId}", user.Id);
            }
            return null;
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.