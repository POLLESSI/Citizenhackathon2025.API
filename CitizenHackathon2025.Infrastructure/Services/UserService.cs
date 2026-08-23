using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class UserService : CitizenHackathon2025.Application.Interfaces.IUserService
    {
    #nullable disable
        private readonly IUserRepository _userRepository;
        private readonly IUserHubService _hubService;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public UserService(IUserRepository userRepository, IUserHubService hubService, IPasswordHasher<Users> passwordHasher)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _hubService = hubService ?? throw new ArgumentNullException(nameof(hubService));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<Users?> AuthenticateAsync(string email,string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var user = await _userRepository.GetUserByEmailAsync(email.Trim());

            if (user is null || !user.Active || user.Status != UserStatus.Active)
            {
                return null;
            }

            /*
             * All supported accounts must now use
             * ASP.NET Core PasswordHasher.
             */
            if (string.IsNullOrWhiteSpace(user.PasswordHashV2))
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHashV2, password);

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            /*
             * ASP.NET Core can request a rehash when
             * password hashing parameters evolve.
             */
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var rehashed = _passwordHasher.HashPassword(user, password);

                await _userRepository.UpdatePasswordHashV2Async(user.Id, rehashed);

                user.PasswordHashV2 = rehashed;
            }

            return user;
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
            var user = await AuthenticateAsync(email, password);

            return user is not null;
        }
        public async Task<UserDTO> RegisterUserAsync(string email, string password, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            email = email.Trim();

            var existing = await _userRepository.GetUserByEmailAsync(email);

            if (existing is not null)
            {
                throw new InvalidOperationException($"User '{email}' already exists.");
            }

            var newUser =
                new Users
                {
                    Email = email,
                    Role = role,
                    SecurityStamp = Guid.NewGuid(),
                    Status = UserStatus.Active,

                    /*
                     * No legacy hash for new accounts.
                     */
                    PasswordHash = null
                };

            newUser.Activate();

            newUser.PasswordHashV2 = _passwordHasher.HashPassword(newUser, password);

            var created = await _userRepository.RegisterUserAsync(newUser);

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