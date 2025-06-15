using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;
using System.Globalization;

namespace Citizenhackathon2025.Application.Extensions
{
    public static class MapperExtensions
    {
        // DTO -> Entity
        public static Citizenhackathon2025.Domain.Entities.WeatherForecast MapToWeatherForecast(this WeatherForecastDTO dto)
        {
            return new Citizenhackathon2025.Domain.Entities.WeatherForecast
            {
                DateWeather = dto.DateWeather,
                TemperatureC = 0,
                Summary = dto.Summary,
                RainfallMm = 0,
                Humidity = 0,
                WindSpeedKmh = 0
                // Id et Active sont gérés par la base
            };
        }

        // Entity -> DTO
        public static WeatherForecastDTO MapToWeatherForecastDTO(this Citizenhackathon2025.Domain.Entities.WeatherForecast entity)
        {
        #nullable disable
            return new WeatherForecastDTO
            {
                Id = entity.Id, // facultatif si API ne doit pas exposer l'Id
                DateWeather = entity.DateWeather,
                TemperatureC = entity.TemperatureC,
                Summary = entity.Summary,
                RainfallMm = entity.RainfallMm,
                Humidity = entity.Humidity,
                WindSpeedKmh = entity.WindSpeedKmh
            };
        }

        // Exemple concret pour un Event
        public static EventDTO MapToEventDTO(this Event entity)
        {
            return new EventDTO
            {
                Name = entity.Name,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DateEvent = entity.DateEvent,
                ExpectedCrowd = entity.ExpectedCrowd,
                IsOutdoor = entity.IsOutdoor
            };
        }

        // CrowdInfo DTO → avec timestamp
        public static CrowdInfoDTO MapToCrowdInfoWithTimestamp(this CrowdInfoDTO dto)
        {
            return new CrowdInfoDTO
            {
                LocationName = dto.LocationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CrowdLevel = dto.CrowdLevel,
                Timestamp = DateTime.UtcNow
            };
        }
        public static CrowdInfo MapToCrowdInfo(this CrowdInfoDTO dto)
        {
            return new CrowdInfo
            {
                LocationName = dto.LocationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CrowdLevel = dto.CrowdLevel,
                Timestamp = dto.Timestamp // ou DateTime.UtcNow directement ici
            };
        }
        public static CrowdInfoDTO MapToCrowdInfoDTO(this CrowdInfo entity)
        {
            if (entity == null) return null;

            return new CrowdInfoDTO
            {
                LocationName = entity.LocationName,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                CrowdLevel = entity.CrowdLevel,
                Timestamp = entity.Timestamp
            };
        }

        // Place → DTOSugges
        public static PlaceDTO MapToPlaceDTO(this Place entity)
        {
            return new PlaceDTO
            {
                Name = entity.Name,
                Type = entity.Type,
                Indoor = entity.Indoor,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                Capacity = entity.Capacity,
                Tag = entity.Tag,
            };
        }

        // Suggestion → DTO
        public static SuggestionDTO MapToSuggestionDTO(this Suggestion entity)
        {
            return new SuggestionDTO
            {
                UserId = entity.UserId,
                DateSuggestion = entity.DateSuggestion,
                OriginalPlace = entity.OriginalPlace,
                SuggestedAlternatives = entity.SuggestedAlternatives,
                Reason = entity.Reason
            };
        }

        // TrafficCondition → DTO
        public static TrafficConditionDTO MapToTrafficConditionDTO(this TrafficCondition entity)
        {
            return new TrafficConditionDTO
            {
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DateCondition = entity.DateCondition,
                CongestionLevel = entity.CongestionLevel,
                IncidentType = entity.IncidentType
            };
        }
    }
}
