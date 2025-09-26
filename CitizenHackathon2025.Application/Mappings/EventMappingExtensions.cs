using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;
using CitizenHackathon2025.Application.Extensions;

namespace CitizenHackathon2025.Application.Mappings
{
    public static class EventMappingExtensions
    {
        public static EventDTO ToDTO(this Event entity) => entity.MapToEventDTO();
        public static Event ToEntity(this EventDTO dto) => dto.MapToEvent();
        public static EventDTO WithDateEvent(this EventDTO dto) => dto.MapToEventWithDateEvent();
    }
}
