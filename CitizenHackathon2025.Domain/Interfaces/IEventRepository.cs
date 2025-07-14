using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Domain.Interfaces
{
    public interface IEventRepository
    {
    #nullable disable
        Task<IEnumerable<Event?>> GetLatestEventAsync();
        Task<Event> SaveEventAsync(Event @event);
        Task<Event> CreateEventAsync(Event newEvent);
        Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync();
        Task<Event?> GetByIdAsync(int id);
        Task<int> ArchivePastEventsAsync();
        Event UpdateEvent(Event @event);
    }
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.