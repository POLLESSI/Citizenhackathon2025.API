using System.Threading.Tasks;

namespace CitizenHackathon2025.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(string message);
        Task NotifyEventAsync(string type, string message);
    }
}
