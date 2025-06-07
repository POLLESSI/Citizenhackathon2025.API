using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using static Citizenhackathon2025.Domain.Entities.Event;

namespace Citizenhackathon2025.Application.Interfaces
{
    public interface IEventService
    {
#nullable disable
        Task<IEnumerable<Event?>> GetLatestEventAsync();
        Task<Event> SaveEventAsync(Event @event);
        Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync();
        Task<Event> CreateEventAsync(Event newEvent);
        Task<Event?> GetByIdAsync(int id);
        Task<int> ArchivePastEventsAsync();
        Event UpdateEvent(Event @event);
    }
}
