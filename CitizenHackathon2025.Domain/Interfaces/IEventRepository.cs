using CitizenHackathon2025.Domain.Entities;

namespace CitizenHackathon2025.Domain.Interfaces
{
    public interface IEventRepository
    {
    #nullable disable
        Task<IEnumerable<Event?>> GetLatestEventAsync(int limit = 10, CancellationToken ct = default);
        Task<Event> SaveEventAsync(Event @event);
        Task<Event> CreateEventAsync(Event newEvent);
        Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync();
        Task<Event?> GetByIdAsync(int id);
        Task<int> ArchivePastEventsAsync();
        Event UpdateEvent(Event @event);
    }
}






















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.