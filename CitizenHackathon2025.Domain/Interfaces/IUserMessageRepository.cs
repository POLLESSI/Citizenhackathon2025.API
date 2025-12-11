using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IUserMessageRepository
    {
        Task<UserMessage> InsertAsync(UserMessage msg, CancellationToken ct = default);
        Task<List<UserMessage>> GetLatestAsync(int take = 100, CancellationToken ct = default);
        Task<UserMessage?> GetByIdAsync(int id, CancellationToken ct = default);
    }
}
