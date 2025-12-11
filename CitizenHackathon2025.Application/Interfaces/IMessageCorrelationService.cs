using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IMessageCorrelationService
    {
        Task<UserMessage> CorrelateAsync(UserMessage raw, CancellationToken ct = default);
    }
}
