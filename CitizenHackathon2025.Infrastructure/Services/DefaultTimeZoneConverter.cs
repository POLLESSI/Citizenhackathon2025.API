using CitizenHackathon2025.Domain.Abstractions;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class DefaultTimeZoneConverter : ITimeZoneConverter
    {
        public DateTime ToUtc(DateTime local, string tz)
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), tzInfo);
        }
        public DateTime ToLocal(DateTime utc, string tz)
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tzInfo);
        }
    }
}
