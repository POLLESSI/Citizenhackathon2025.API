using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class GeoService : IGeoService
    {
        public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string location)
        {
            var dict = new Dictionary<string, (double, double)>
            {
                ["Paris"] = (48.8566, 2.3522),
                ["Lyon"] = (45.75, 4.85),
                ["Marseille"] = (43.2965, 5.3698)
            };

            if (dict.TryGetValue(location, out var coords))
                return await Task.FromResult<(double, double)?>(coords);

            return await Task.FromResult<(double, double)?>(null);
        }
    }
}
