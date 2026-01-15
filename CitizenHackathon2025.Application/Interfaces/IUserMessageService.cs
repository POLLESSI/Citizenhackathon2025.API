using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IUserMessageService
    {
        Task<UserMessage> InsertAsync(UserMessage msg, CancellationToken ct = default);
        Task<List<UserMessage>> GetLatestAsync(int take = 100, CancellationToken ct = default);
        Task<UserMessage?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<bool> DeleteMessageAsync(int id, CancellationToken ct = default);
    }
}





























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.