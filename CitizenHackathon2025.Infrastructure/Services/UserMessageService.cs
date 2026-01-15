using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    /// <summary>
    /// Lightweight application service on top of the repository :
    /// - Minimum validation
    /// - Normalization (trim, take terminals, etc.)
    /// - Delegate data access to the repository (Dapper)
    /// </summary>
    public sealed class UserMessageService : IUserMessageService
    {
        private readonly IUserMessageRepository _repo;

        public UserMessageService(IUserMessageRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserMessage> InsertAsync(UserMessage msg, CancellationToken ct = default)
        {
            if (msg is null) throw new ArgumentNullException(nameof(msg));

            if (string.IsNullOrWhiteSpace(msg.Content))
                throw new ArgumentException("Content cannot be empty.", nameof(msg));

            // Standardization to avoid surprises on the DB/UI side
            msg.Content = msg.Content.Trim();

            // Your SQL schema allows UserId NULL, but your entity declares it non-null.
            // We secure it to avoid null refs and to keep data consistent.
            msg.UserId = string.IsNullOrWhiteSpace(msg.UserId) ? "anon" : msg.UserId.Trim();

            // Same logic: you have non-null strings in the entity but NULL columns in the DB.
            // We avoid pushing accidental losers.
            msg.SourceType = string.IsNullOrWhiteSpace(msg.SourceType) ? "Other" : msg.SourceType.Trim();
            msg.RelatedName = string.IsNullOrWhiteSpace(msg.RelatedName) ? null : msg.RelatedName.Trim();
            msg.Tags = string.IsNullOrWhiteSpace(msg.Tags) ? null : msg.Tags.Trim();

            // Latitude/Longitude: nothing to impose here, but you could add a validation if needed.

            return await _repo.InsertAsync(msg, ct);
        }

        public Task<List<UserMessage>> GetLatestAsync(int take = 100, CancellationToken ct = default)
        {
            // Anti-abuse and anti-“take=999999” terminals
            if (take <= 0) take = 10;
            if (take > 500) take = 500;

            return _repo.GetLatestAsync(take, ct);
        }

        public Task<UserMessage?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0) return Task.FromResult<UserMessage?>(null);
            return _repo.GetByIdAsync(id, ct);
        }

        public Task<bool> DeleteMessageAsync(int id, CancellationToken ct = default)
        {
            if (id <= 0) return Task.FromResult(false);
            return _repo.DeleteMessageAsync(id, ct);
        }
    }
}
























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.