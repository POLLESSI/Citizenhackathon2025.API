using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.DTOs.DTOs;

namespace CitizenHackathon2025.Infrastructure.Services
{
    public sealed class CrowdInfoAntennaService : ICrowdInfoAntennaService
    {
        private readonly ICrowdInfoAntennaRepository _antRepo;
        private readonly ICrowdInfoAntennaConnectionRepository _connRepo;

        // Dependency on EventRepository/service (you already have it in OutZen)
        private readonly IEventReadService _eventRead; // To create/adapt: ​​GetById -> lat/lng

        public CrowdInfoAntennaService(
            ICrowdInfoAntennaRepository antRepo,
            ICrowdInfoAntennaConnectionRepository connRepo,
            IEventReadService eventRead)
        {
            _antRepo = antRepo;
            _connRepo = connRepo;
            _eventRead = eventRead;
        }

        public async Task<IReadOnlyList<CrowdInfoAntennaDTO>> GetAllAsync(CancellationToken ct)
        {
            var all = await _antRepo.GetAllAsync(ct);
            return all.Select(a => new CrowdInfoAntennaDTO
            {
                Id = a.Id,
                Name = a.Name,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                CreatedUtc = a.CreatedUtc,
                Description = a.Description
            }).ToList();
        }

        public async Task<CrowdInfoAntennaDTO?> GetByIdAsync(int id, CancellationToken ct)
        {
            var a = await _antRepo.GetByIdAsync(id, ct);
            if (a is null) return null;

            return new CrowdInfoAntennaDTO
            {
                Id = a.Id,
                Name = a.Name,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                CreatedUtc = a.CreatedUtc,
                Description = a.Description
            };
        }

        public async Task<NearestAntennaDTO?> GetNearestAsync(double lat, double lng, double maxRadiusMeters, CancellationToken ct)
        {
            var nearest = await _antRepo.GetNearestAsync(lat, lng, maxRadiusMeters, ct);
            if (nearest is null) return null;

            var (a, dist) = nearest.Value;
            return new NearestAntennaDTO
            {
                Antenna = new CrowdInfoAntennaDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    CreatedUtc = a.CreatedUtc,
                    Description = a.Description
                },
                DistanceMeters = dist
            };
        }

        public async Task<AntennaCountsDTO> GetCountsAsync(int antennaId, int windowMinutes, CancellationToken ct)
        {
            var end = DateTime.UtcNow;
            var start = end.AddMinutes(-Math.Abs(windowMinutes));

            var (activeConnections, uniqueDevices) = await _connRepo.GetCountsAsync(antennaId, start, end, ct);

            return new AntennaCountsDTO
            {
                ActiveConnections = activeConnections,
                UniqueDevices = uniqueDevices,
                WindowMinutes = Math.Abs(windowMinutes),
                WindowStartUtc = start,
                WindowEndUtc = end
            };
        }

        public async Task<EventAntennaCrowdDTO?> GetEventCrowdAsync(int eventId, int windowMinutes, double maxRadiusMeters, CancellationToken ct)
        {
            var ev = await _eventRead.GetEventGeoAsync(eventId, ct);
            if (ev is null) return null;

            var nearest = await GetNearestAsync(ev.Value.Latitude, ev.Value.Longitude, maxRadiusMeters, ct);
            if (nearest is null) return null;

            var counts = await GetCountsAsync(nearest.Antenna.Id, windowMinutes, ct);

            return new EventAntennaCrowdDTO
            {
                EventId = eventId,
                AntennaId = nearest.Antenna.Id,
                DistanceMeters = nearest.DistanceMeters,
                Counts = counts
            };
        }
    }

    // Small, minimal contract (to connect to your existing Place/Event)
    public interface IEventReadService
    {
        Task<(double Latitude, double Longitude)?> GetEventGeoAsync(int eventId, CancellationToken ct);
    }
}
