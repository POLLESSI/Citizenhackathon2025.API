using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class UserMessageService : IUserMessageService
    {
        private readonly IUserMessageRepository _repo;
        private readonly IProfanityService _profanityService;

        public UserMessageService(
            IUserMessageRepository repo,
            IProfanityService profanityService)
        {
            _repo = repo;
            _profanityService = profanityService;
        }

        public async Task<UserMessage> InsertAsync(UserMessage msg, CancellationToken ct = default)
        {
            if (msg is null)
                throw new ArgumentNullException(nameof(msg));

            if (string.IsNullOrWhiteSpace(msg.Content))
                throw new ArgumentException("Content cannot be empty.", nameof(msg));

            msg.Content = msg.Content.Trim();

            var analysis = await _profanityService.AnalyzeAsync(msg.Content, ct);

            if (analysis.ShouldReject)
                throw new ArgumentException(
                    $"The message contains prohibited content. Score={analysis.Score}.",
                    nameof(msg.Content));

            msg.UserId = string.IsNullOrWhiteSpace(msg.UserId) ? "anon" : msg.UserId.Trim();
            msg.SourceType = string.IsNullOrWhiteSpace(msg.SourceType) ? "Other" : msg.SourceType.Trim();
            msg.RelatedName = string.IsNullOrWhiteSpace(msg.RelatedName) ? null : msg.RelatedName.Trim();
            msg.Tags = string.IsNullOrWhiteSpace(msg.Tags) ? null : msg.Tags.Trim();

            return await _repo.InsertAsync(msg, ct);
        }

        public Task<List<UserMessage>> GetLatestAsync(int take = 100, CancellationToken ct = default)
        {
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