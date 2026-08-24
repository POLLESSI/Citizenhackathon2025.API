using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using Dapper;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class UserRepository : CitizenHackathon2025.Domain.Interfaces.IUserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
            // Type handler to properly map UserRole <-> int
            SqlMapper.AddTypeHandler(new RoleTypeHandler());
        }

        // =========================================
        // READ
        // =========================================
        public Task<Users?> GetUserByEmailAsync(string email)
        {
            const string sql = """
                            SELECT TOP(1)
                                Id,
                                Email,
                                SecurityStamp,
                                PasswordHashV2,
                                Role,
                                Status,
                                Active
                            FROM dbo.Users
                            WHERE Email = @Email;
                            """;

            return _connection.QueryFirstOrDefaultAsync<Users>(sql, new { Email = email.Trim() });
        }

        public Task<Users?> GetUserByIdAsync(int id)
        {
            const string sql = """
                            SELECT TOP(1)
                                Id,
                                Email,
                                SecurityStamp,
                                PasswordHashV2,
                                Role,
                                Status,
                                Active
                            FROM dbo.Users
                            WHERE Id = @Id;
                            """;

            return _connection.QueryFirstOrDefaultAsync<Users>( sql, new { Id = id });
        }

        public Task<IEnumerable<Users>> GetAllActiveUsersAsync()
        {
            const string sql = """
                            SELECT
                                Id,
                                Email,
                                SecurityStamp,
                                PasswordHashV2,
                                Role,
                                Status,
                                Active
                            FROM dbo.Users
                            WHERE Active = 1
                            ORDER BY Id DESC;
                            """;

            return _connection.QueryAsync<Users>(sql);
        }

        // =========================================
        // CREATE (registration)
        // =========================================
        // NB : Here we use the hash passed by the caller + a SecurityStamp
        // (If you prefer to force the use of the SP sqlUserRegister, change the signature on the service side)
        public async Task<Users> RegisterUserAsync(Users user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(user));
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHashV2))
            {
                throw new ArgumentException("PasswordHashV2 cannot be empty.", nameof(user));
            }

            var stamp = user.SecurityStamp == Guid.Empty ? Guid.NewGuid() : user.SecurityStamp;

            const string sql = """
                            INSERT INTO dbo.Users
                            (
                                Email,
                                PasswordHashV2,
                                SecurityStamp,
                                Role,
                                Status,
                                Active
                            )
                            VALUES
                            (
                                @Email,
                                @PasswordHashV2,
                                @SecurityStamp,
                                @Role,
                                @Status,
                                1
                            );
                            """;

            var parameters = new DynamicParameters();

            parameters.Add("@Email", user.Email.Trim(), DbType.String, size: 64);
            parameters.Add("@PasswordHashV2", user.PasswordHashV2, DbType.String, size: 512);
            parameters.Add("@SecurityStamp", stamp, DbType.Guid);
            parameters.Add("@Role", (int)user.Role, DbType.Int32);
            parameters.Add("@Status", (int)UserStatus.Active, DbType.Int32);

            await _connection.ExecuteAsync(sql, parameters);

            var created = await GetUserByEmailAsync(user.Email.Trim());

            return created ?? throw new InvalidOperationException("User insert failed unexpectedly.");
        }

        public async Task AnonymizeUserAsync(int userId, CancellationToken ct = default)
        {
            const string sql = """
                            UPDATE dbo.Users
                            SET
                                Email = CONCAT('deleted-', Id,'@example.com'),
                                PasswordHashV2 = NULL,
                                SecurityStamp = NEWID()
                            WHERE Id = @UserId;
                            """;

            await _connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserId = userId
                    },
                    cancellationToken: ct));
        }

        // =========================================
        // UPDATE / COMMANDES
        // =========================================
        public Task DeactivateUserAsync(int id)
        {
            const string sql = @"UPDATE [Users] SET Active = 0 WHERE Id = @Id;";
            return _connection.ExecuteAsync(sql, new { Id = id });
        }

        public void SetRole(int id, string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return;

            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Invalid role '{role}'.", nameof(role));

            const string sql = @"UPDATE [Users] SET Role = @Role WHERE Id = @Id;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);
            parameters.Add("@Role", (int)parsed, DbType.Int32);

            _connection.Execute(sql, parameters);
        }

        public Users? UpdateUser(Users user)
        {
            ArgumentNullException.ThrowIfNull(user);

            const string sql = """
                            UPDATE dbo.Users
                            SET
                                Email = @Email,
                                Role = @Role,
                                Status = @Status,
                                Active = @Active
                            WHERE Id = @Id;

                            IF @@ROWCOUNT = 0
                                RETURN;

                            SELECT TOP(1)
                                Id,
                                Email,
                                SecurityStamp,
                                PasswordHashV2,
                                Role,
                                Status,
                                Active
                            FROM dbo.Users
                            WHERE Id = @Id;
                            """;

            var parameters = new DynamicParameters();

            parameters.Add("@Id", user.Id, DbType.Int32);
            parameters.Add("@Email", user.Email, DbType.String, size: 64);
            parameters.Add("@Role", (int)user.Role, DbType.Int32);
            parameters.Add("@Status", (int)user.Status, DbType.Int32);
            parameters.Add("@Active", user.Active, DbType.Boolean);

            return _connection.QueryFirstOrDefault<Users>(sql, parameters);
        }

        // =========================================
        // PASSWORD HASH
        // =========================================

        public async Task UpdatePasswordHashV2Async(int userId, string passwordHashV2)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(passwordHashV2))
            {
                throw new ArgumentException("Password hash V2 cannot be empty.", nameof(passwordHashV2));
            }

            const string sql = """
                            UPDATE dbo.Users
                            SET PasswordHashV2 = @PasswordHashV2
                            WHERE Id = @UserId;
                            """;

            var affected = await _connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    PasswordHashV2 = passwordHashV2
                });

            if (affected != 1)
            {
                throw new InvalidOperationException($"Unable to update PasswordHashV2 " + $"for user {userId}. " + $"Affected rows: {affected}.");
            }
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.