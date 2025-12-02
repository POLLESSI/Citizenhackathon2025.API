using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Infrastructure.Dapper.TypeHandlers;

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
            const string sql = @"
                        SELECT TOP(1) Id, Email, SecurityStamp, PasswordHash, Role, Status, Active
                        FROM [Users]
                        WHERE Email = @Email;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Email", email, DbType.String, size: 64);

            return _connection.QueryFirstOrDefaultAsync<Users>(sql, parameters);
        }

        public Task<Users?> GetUserByIdAsync(int id)
        {
            const string sql = @"
                        SELECT TOP(1) Id, Email, SecurityStamp, PasswordHash, Role, Status, Active
                        FROM [Users]
                        WHERE Id = @Id;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", id, DbType.Int32);
            parameters.Add("@PasswordHash", dbType: DbType.Binary, size: 64);
            parameters.Add("@SecurityStamp", dbType: DbType.Guid);
            parameters.Add("@Role", dbType: DbType.Int32);
            parameters.Add("@Status", dbType: DbType.Int32);

            return _connection.QueryFirstOrDefaultAsync<Users>(sql, parameters);
        }

        public Task<IEnumerable<Users>> GetAllActiveUsersAsync()
        {
            const string sql = @"
                        SELECT Id, Email, SecurityStamp, PasswordHash, Role, Status, Active
                        FROM [Users]
                        WHERE Active = 1
                        ORDER BY Id DESC;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", dbType: DbType.Int32);
            parameters.Add("@Email", dbType: DbType.String, size: 64);
            parameters.Add("@SecurityStamp", dbType: DbType.Guid);
            parameters.Add("@PasswordHash", dbType: DbType.Binary, size: 64);
            parameters.Add("@Role", dbType: DbType.Int32);
            parameters.Add("@Status", dbType: DbType.Int32);

            return _connection.QueryAsync<Users>(sql, parameters);
        }

        // =========================================
        // CREATE (registration)
        // =========================================
        // NB : Here we use the hash passed by the caller + a SecurityStamp
        // (If you prefer to force the use of the SP sqlUserRegister, change the signature on the service side)
        public async Task<Users> RegisterUserAsync(string email, byte[] passwordHash, Users user)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.", nameof(email));

            if (passwordHash == null || passwordHash.Length == 0)
                throw new ArgumentException("PasswordHash cannot be empty.", nameof(passwordHash));

            // SecurityStamp: if not filled in, one is created.
            var stamp = user.SecurityStamp == Guid.Empty ? Guid.NewGuid() : user.SecurityStamp;

            const string insertSql = @"
                            INSERT INTO [Users] (Email, PasswordHash, SecurityStamp, Role, Status, Active)
                            VALUES (@Email, @PasswordHash, @SecurityStamp, @Role, @Status, 1);
                            ";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Email", email.Trim(), DbType.String, size: 64);
            parameters.Add("@PasswordHash", passwordHash, DbType.Binary, size: 64);
            parameters.Add("@SecurityStamp", stamp, DbType.Guid);
            parameters.Add("@Role", (int)user.Role, DbType.Int32); // enum -> int
            parameters.Add("@Status", (int)UserStatus.Active, DbType.Int32); 

            await _connection.ExecuteAsync(insertSql, parameters);

            // Return the user from the DB (avoids private setter issues)
            var created = await GetUserByEmailAsync(email.Trim());
            if (created is null)
                throw new InvalidOperationException("User insert failed unexpectedly.");
            return created;
        }

        // =========================================
        // LOGIN - Variante code (hash C# compatible SQL)
        // =========================================
        public async Task<bool> LoginAsync(string email, string password)
        {
            // User recovery
            var user = await GetUserByEmailAsync(email);
            if (user is null) return false;
            if (!user.Active) return false;
            if (user.Status != UserStatus.Active) return false;

            // Recalculate HASH in SQL way (NVARCHAR -> UTF-16 LE)
            var expected = ComputeSqlLikeSha512(password, user.SecurityStamp);

            // Constant time comparison
            var ok = user.PasswordHash is not null
                && user.PasswordHash.Length == expected.Length
                && CryptographicOperations.FixedTimeEquals(user.PasswordHash, expected);

            return ok;
        }

        // =========================================
        // LOGIN - Variant via the dbo.sqlUserLogin stored procedure
        // Returns the full user if successful, otherwise null
        // =========================================
        public async Task<Users?> LoginUsingProcedureAsync(string email, string password)
        {
            var p = new DynamicParameters();
            p.Add("@Email", email, DbType.String, size: 64);
            p.Add("@Password", password, DbType.String, size: 64);

            // The SP returns (Id, Email, Role, Active) if OK; nothing otherwise
            // We don't get everything back here: we do a complete SELECT again if successful.
            var minimal = await _connection.QueryFirstOrDefaultAsync(
                new CommandDefinition("dbo.sqlUserLogin", p, commandType: CommandType.StoredProcedure));

            if (minimal == null) return null;

            // We return the complete entity
            return await GetUserByEmailAsync(email);
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
            const string sql = @"
                            UPDATE [Users]
                            SET Email=@Email, Role=@Role, Status=@Status, Active=@Active
                            WHERE Id=@Id;

                            IF @@ROWCOUNT = 0
                                RETURN;

                            SELECT TOP(1) Id, Email, SecurityStamp, PasswordHash, Role, Status, Active
                            FROM [Users]
                            WHERE Id=@Id;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Id", user.Id, DbType.Int32);
            parameters.Add("@Email", user.Email, DbType.String, size: 64);
            parameters.Add("@Role", (int)user.Role, DbType.Int32);
            parameters.Add("@Status", (int)user.Status, DbType.Int32);
            parameters.Add("@Active", user.Active, DbType.Boolean);

            return _connection.QueryFirstOrDefault<Users>(sql, parameters);
        }

        // =========================================
        // Helpers
        // =========================================

        /// <summary>
        /// Reproduced HASHBYTES('SHA2_512', @Password + CONVERT(NVARCHAR(36), @SecurityStamp))
        /// NVARCHAR -> UTF-16 THE (Encoding.Unicode).
        /// </summary>
        private static byte[] ComputeSqlLikeSha512(string? password, Guid securityStamp)
        {
            var combined = (password ?? string.Empty).Trim() + securityStamp.ToString();
            var bytes = Encoding.Unicode.GetBytes(combined); // UTF-16 LE
            return SHA512.HashData(bytes);
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.