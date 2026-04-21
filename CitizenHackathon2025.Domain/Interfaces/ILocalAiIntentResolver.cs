using CitizenHackathon2025.Domain.Models;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ILocalAiIntentResolver
    {
        LocalAiContextIntent Resolve(string prompt);
    }
}
