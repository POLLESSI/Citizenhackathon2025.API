namespace CitizenHackathon2025.Shared.Interfaces
{
    public interface IDailyArchiverOptions
    {
        string TimeZone { get; }
        int Hour { get; }
        int Minute { get; }
    }
}
