namespace CitizenHackathon2025.Domain.Abstractions
{
    public interface ITimeZoneConverter
    {
        DateTime ToUtc(DateTime local, string ianaOrWindowsTz);
        DateTime ToLocal(DateTime utc, string ianaOrWindowsTz);
    }
}
