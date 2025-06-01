using Microsoft.AspNetCore.SignalR.Client;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Application;
using Citizenhackathon2025.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;

namespace Citizenhackathon2025.Application.Services
{
    public class EventService : IEventService
    {
    #nullable disable
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }
        public async Task<Event> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("The event ID must be greater than zero.", nameof(id));
            }

            var eventEntity = await _eventRepository.GetByIdAsync(id);

            if (eventEntity == null || !eventEntity.Active)
            {
                return null;
            }

            return eventEntity;
        }
        public async Task<IEnumerable<Event>> GetLatestEventAsync()
        {
            var events = await _eventRepository.GetLatestEventAsync();
            return events;
        }

        public async Task<IEnumerable<Event>> GetUpcomingOutdoorEventsAsync()
        {
            var events = await _eventRepository.GetLatestEventAsync();
            return events.Where(e => e.DateEvent > DateTime.Now);
        }

        public async Task<Event> SaveEventAsync(Event @event)
        {
            return await _eventRepository.SaveEventAsync(@event);
        }
        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            if (newEvent == null)
                throw new ArgumentNullException(nameof(newEvent));

            return await _eventRepository.CreateEventAsync(newEvent);
        }
    }
}
