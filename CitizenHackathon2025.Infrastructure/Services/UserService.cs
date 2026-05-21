using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Shared.Utils;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Data;
using System.Security.Cryptography;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class UserService : CitizenHackathon2025.Application.Interfaces.IUserService
    {
    #nullable disable
        private readonly IUserRepository _userRepository;
        private readonly IUserHubService _hubService;
        private readonly ILogger<UserService> _logger;
        private readonly IDbConnection _connection;

        public UserService(IUserRepository userRepository, IUserHubService hubService, ILogger<UserService> logger, IDbConnection connection)
        {
            _userRepository = userRepository;
            _hubService = hubService;
            _logger = logger;
            _connection = connection;
        }

        public async Task<Users?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email.Trim());

            if (user is null)
                return null;

            if (!user.Active || user.Status != UserStatus.Active)
                return null;

            var expected = Sql512Hasher.Compute(password, user.SecurityStamp);

            var ok = user.PasswordHash is not null
                     && user.PasswordHash.Length == expected.Length
                     && CryptographicOperations.FixedTimeEquals(user.PasswordHash, expected);

            return ok ? user : null;
        }

        public async Task DeactivateUserAsync(int id)
        {
            await _userRepository.DeactivateUserAsync(id);
            await _hubService.NotifyUserDeactivated(id);
        }

        public async Task<IEnumerable<Users>> GetAllActiveUsersAsync()
            => await _userRepository.GetAllActiveUsersAsync();

        public async Task<Users> GetUserByEmailAsync(string email)
            => await _userRepository.GetUserByEmailAsync(email);

        public async Task<Users> GetUserByIdAsync(int id)
            => await _userRepository.GetUserByIdAsync(id);

        public async Task<bool> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email.Trim());

            if (user is null)
                return false;

            if (!user.Active || user.Status != UserStatus.Active)
                return false;

            var expected = Sql512Hasher.Compute(password, user.SecurityStamp);

            return user.PasswordHash is not null
                   && user.PasswordHash.Length == expected.Length
                   && CryptographicOperations.FixedTimeEquals(user.PasswordHash, expected);
        }
        public async Task<UserDTO> RegisterUserAsync(string email, string password, UserRole role)
        {
            email = email.Trim();

            var existing = await _userRepository.GetUserByEmailAsync(email);
            if (existing is not null)
                throw new InvalidOperationException($"User '{email}' already exists.");

            var stamp = Guid.NewGuid();
            var passwordHash = Sql512Hasher.Compute(password, stamp);

            var newUser = new Users
            {
                Email = email,
                Role = role,
                SecurityStamp = stamp,
                PasswordHash = passwordHash,
                Status = UserStatus.Active
            };

            newUser.Activate();

            var created = await _userRepository.RegisterUserAsync(email, passwordHash, newUser);

            await _hubService.NotifyUserRegistered(created.Email);

            return new UserDTO
            {
                Id = created.Id,
                Email = created.Email,
                Role = created.Role.ToString(),
                Active = created.Active
            };
        }
        public void SetRole(int id, string role)
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
                throw new ArgumentException("Invalid role format", nameof(role));

            _userRepository.SetRole(id, parsedRole.ToString());
        }

        public Users UpdateUser(Users user)
           => _userRepository.UpdateUser(user);
    }
}

































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.