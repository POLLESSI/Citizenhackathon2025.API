using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Enums;
using CitizenHackathon2025.Domain.ValueObjects;
using CitizenHackathon2025.DTOs.DTOs;
using Volo.Abp.Domain.Entities;

namespace CitizenHackathon2025.Application.Extensions
{
    public static class MapperExtensions
    {
    #nullable disable
        // DTO -> Entity
        public static CitizenHackathon2025.Domain.Entities.WeatherForecast MapToWeatherForecast(this WeatherForecastDTO dto)
        {
            return new CitizenHackathon2025.Domain.Entities.WeatherForecast
            {
                DateWeather = dto.DateWeather,
                TemperatureC = dto.TemperatureC,
                Summary = dto.Summary,
                RainfallMm = dto.RainfallMm,
                Humidity = dto.Humidity,
                WindSpeedKmh = dto.WindSpeedKmh
            };
        }

        // Entity -> DTO
        public static WeatherForecastDTO MapToWeatherForecastDTO(this CitizenHackathon2025.Domain.Entities.WeatherForecast entity)
        {

            return new WeatherForecastDTO
            {
                Id = entity.Id, 
                DateWeather = entity.DateWeather,
                TemperatureC = entity.TemperatureC,
                Summary = entity.Summary,
                RainfallMm = entity.RainfallMm,
                Humidity = entity.Humidity,
                WindSpeedKmh = entity.WindSpeedKmh
            };
        }

        public static WeatherInfoDTO MapToWeatherInfoDTO(this WeatherForecastDTO dto, string city)
        {
            return new WeatherInfoDTO
            {
                Location = city,
                TemperatureCelsius = dto.TemperatureC,
                FeelsLikeCelsius = dto.TemperatureC, // Adjust if you have real data
                WeatherDescription = dto.Summary,
                WindSpeedKmh = dto.WindSpeedKmh,
                HumidityPercent = dto.Humidity,
                RetrievedAt = dto.DateWeather
                // Sunrise / Sunset : only if available elsewhere
            };
        }

        // Concrete example for an Event
        public static EventDTO MapToEventDTO(this Event entity)
        {
            return new EventDTO
            {
                Name = entity.Name,
                Latitude = (double)entity.Latitude,
                Longitude = (double)entity.Longitude,
                DateEvent = entity.DateEvent,
                ExpectedCrowd = entity.ExpectedCrowd,
                IsOutdoor = entity.IsOutdoor
            };
        }

        // CrowdInfo mappings (digital)
        public static CrowdInfoDTO MapToCrowdInfoDTO(this CrowdInfo entity)
        {
            if (entity is null) return null!;
            return new CrowdInfoDTO
            {
                Id = entity.Id,
                LocationName = entity.LocationName,
                Latitude = (double)entity.Latitude,    // decimal -> double
                Longitude = (double)entity.Longitude,  // decimal -> double
                CrowdLevel = entity.CrowdLevel,
                Timestamp = entity.Timestamp
            };
        }

        public static CrowdInfo MapToCrowdInfo(this CrowdInfoDTO dto)
        {
            if (dto is null) return null!;
            return new CrowdInfo
            {
                Id = dto.Id,
                LocationName = dto.LocationName,
                Latitude = (decimal)dto.Latitude,     // double -> decimal
                Longitude = (decimal)dto.Longitude,   // double -> decimal
                CrowdLevel = dto.CrowdLevel,          
                Timestamp = dto.Timestamp
            };
        }

        public static CrowdInfoDTO MapToCrowdInfoWithTimestamp(this CrowdInfoDTO dto)
            => new()
            {
                //Id = dto.Id,
                LocationName = dto.LocationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CrowdLevel = dto.CrowdLevel,
                Timestamp = DateTime.UtcNow
            }
;


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
                UserId = entity.User_Id,
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
        public static TrafficCondition MapToTrafficCondition(this TrafficConditionUpdateDTO dto)
        {
            var entity = new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType
            };
            return entity.WithId(dto.Id);
        }
        //Mapping vers DTO
        public static UserDTO UserToDTO(this Domain.Entities.Users user)
        {
            /// <summary>
            /// Maps a User entity to a UserDTO (without exposing the PasswordHash).
            /// </summary>
            return new UserDTO
            {
                Email = user.Email,
                Role = user.Role.ToString(),
                Pwd = string.Empty // we never send a password again !
            };
        }

        //Mapping depuis DTO
        /// <summary>
        /// Maps a UserDTO to a User entity (PasswordHash to be managed separately).
        /// </summary>
        public static Domain.Entities.Users MapToUserEntity(this UserDTO dto, Func<string, string, byte[]> hashPasswordFunc, string securityStamp)
        {
            if (!Guid.TryParse(securityStamp, out var parsedStamp))
                throw new ArgumentException("Invalid GUID format for security stamp", nameof(securityStamp));

            var user = new Domain.Entities.Users
            {
                Email = dto.Email,
                PasswordHash = hashPasswordFunc(dto.Pwd, securityStamp),
                SecurityStamp = parsedStamp,
                Role = Enum.Parse<UserRole>(dto.Role, ignoreCase: true),
                Status = Status.Pending.ToUserStatus()
            };

            user.Activate();
            return user;
        }
    }     
}
















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.