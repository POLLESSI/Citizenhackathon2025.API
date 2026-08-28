using CitizenHackathon2025.Contracts.DTOs;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IUserMessageAdminQueueRepository
    {
        Task<int> CreateAsync(UserMessageAdminQueue item, CancellationToken ct = default);
        Task<IReadOnlyList<AdminMessageQueueDto>> GetAsync(AdminMessageQueueFilter filter, CancellationToken ct = default);
        Task<bool> UpdateStatusAsync(int id, AdminMessageStatus status, string? adminNote, string? assignedTo, CancellationToken ct = default);
    }
}
