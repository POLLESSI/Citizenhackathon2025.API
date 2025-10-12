using System.Threading.Tasks;

namespace CitizenHackathon2025.Shared.Notifications
{
    public interface INotifierAdmin
    {
        Task NotifyAdminAsync(object message);
    }
}