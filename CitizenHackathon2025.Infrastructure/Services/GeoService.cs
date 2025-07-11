using CitizenHackathon2025.Application.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public class GeoService : IGeoService
    {
        public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string location)
        {
            var dict = new Dictionary<string, (double, double)>
            {
                ["Brussels"] = (50.846782, 4.352421),
                ["Namur"] = (50.461252, 4.868969),
                ["Han-Sur-Lesse"] = (50.125352, 5.187751)
            };

            if (dict.TryGetValue(location, out var coords))
                return await Task.FromResult<(double, double)?>(coords);

            return await Task.FromResult<(double, double)?>(null);
        }
    }
}
