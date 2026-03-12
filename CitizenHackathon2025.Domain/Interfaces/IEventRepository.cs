using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event?>> GetLatestEventAsync(int limit = 10, CancellationToken ct = default);
        Task<Event> SaveEventAsync(Event @event, CancellationToken ct = default);
        Task<Event> CreateEventAsync(Event newEvent, CancellationToken ct = default);
        Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync(CancellationToken ct = default);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync(double? latitude, double? longitude, int radiusKm, CancellationToken ct = default);
        Task<Event?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<int> ArchivePastEventsAsync(CancellationToken ct = default);
        Event UpdateEvent(Event @event);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.