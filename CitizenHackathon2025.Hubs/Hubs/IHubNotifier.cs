using System.Threading.Tasks;

namespace Citizenhackathon2025.Hubs.Hubs
{
    public interface IHubNotifier
    {
        Task NotifyAsync(string message);
    }
}
