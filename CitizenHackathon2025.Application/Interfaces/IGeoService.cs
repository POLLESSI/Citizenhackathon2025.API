namespace CitizenHackathon2025.Application.Interfaces
{
    public interface IGeoService
    {
        Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string location);
    }
}
