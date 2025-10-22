using CitizenHackathon2025.Shared.Options;

namespace CitizenHackathon2025.Shared.Time
{
    public interface IDelayHelper
    {
        static abstract TimeSpan GetDelayUntilNextRun(DailyArchiverOptions o);
    }
}