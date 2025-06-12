using Citizenhackathon2025.Application;
using Citizenhackathon2025.Application.Common;
using Citizenhackathon2025.Application.Interfaces;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
            if (!Validators.IsFutureOrToday(@event.DateEvent))
                throw new ValidationException("The date must be today or in the future.");

            return await _eventRepository.SaveEventAsync(@event);
        }
        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            if (newEvent == null)
                throw new ArgumentNullException(nameof(newEvent));

            return await _eventRepository.CreateEventAsync(newEvent);
        }

        public Event UpdateEvent(Event @event)
        {
            try
            {
                var UpdateEvent = _eventRepository.UpdateEvent(@event);
                if (UpdateEvent == null)
                {
                    throw new ArgumentException("The event to update cannot be null.", nameof(@event));
                }
                return UpdateEvent;
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event : {ex}");
            }
            return null;
        }

        public async Task<int> ArchivePastEventsAsync()
        {
            string sql = "UPDATE Event SET Active = 0 WHERE DateEvent < @Threshold AND Active = 1";
            var parameters = new { Threshold = DateTime.UtcNow.Date.AddDays(-2) };
            return await _eventRepository.ArchivePastEventsAsync();
        }
    }
}
