using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface ILocalAiIntentResolver
    {
        LocalAiIntent Resolve(string prompt);
    }
}
