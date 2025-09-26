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
        private static decimal RoundLat(double lat) => Math.Round((decimal)lat, 2); // DECIMAL(9,2)
        private static decimal RoundLon(double lon) => Math.Round((decimal)lon, 3); // DECIMAL(9,3)
        private static DateTime TruncateToSecond(DateTime dt)
            => new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerSecond), dt.Kind);

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
        // Entity -> DTO
        public static EventDTO MapToEventDTO(this Event entity)
           => new()
           {
               Id = entity.Id,
               Name = entity.Name,
               Latitude = (double)entity.Latitude,
               Longitude = (double)entity.Longitude,
               DateEvent = entity.DateEvent,               
               ExpectedCrowd = entity.ExpectedCrowd ?? 0,
               IsOutdoor = entity.IsOutdoor
           };
        // DTO -> Entity
        public static Event MapToEvent(this EventDTO dto)
             => new()
             {
                 Id = dto.Id,
                 Name = dto.Name,
                 Latitude = RoundLat(dto.Latitude),          
                 Longitude = RoundLon(dto.Longitude),        
                 DateEvent = TruncateToSecond(dto.DateEvent),
                 ExpectedCrowd = dto.ExpectedCrowd ?? 0,     
                 IsOutdoor = dto.IsOutdoor,
                 Active = true
             };
        public static EventDTO MapToEventWithDateEvent(this EventDTO dto)
            => new()
            {
                Id = dto.Id,
                Name = dto.Name,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateEvent = TruncateToSecond(dto.DateEvent),
                ExpectedCrowd = dto.ExpectedCrowd,
                IsOutdoor = dto.IsOutdoor
            };

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
;       // GPTInteraction → DTO
        public static GptInteractionDTO MapToGptInteractionDTO(this GPTInteraction entity)
        {
            if (entity is null) return null!;
            return new GptInteractionDTO
            {
                Id = entity.Id,
                Prompt = entity.Prompt ?? string.Empty,
                Response = entity.Response ?? string.Empty,
                PromptHash = entity.PromptHash ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                Active = entity.Active
            };
        }
        // DTO → GPTInteraction
        public static GPTInteraction MapToGptInteraction(this GptInteractionDTO dto)
        {
            if (dto is null) return null!;
            return new GPTInteraction
            {
                Id = dto.Id, // if 0, the DB will assign the IDENTITY
                Prompt = dto.Prompt,
                Response = dto.Response,
                PromptHash = string.IsNullOrWhiteSpace(dto.PromptHash) ? null! : dto.PromptHash,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                Active = dto.Active
            };
        }
        // DTO -> Entity partial update)
        public static GPTInteraction UpdateFrom(this GPTInteraction entity, GptInteractionDTO dto)
        {
            if (entity is null || dto is null) return entity!;
            entity.Prompt = dto.Prompt;
            entity.Response = dto.Response;
            if (!string.IsNullOrWhiteSpace(dto.PromptHash))
                entity.PromptHash = dto.PromptHash;
            // CreatedAt: generally immutable (audit)
            entity.Active = dto.Active;
            return entity;
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
        public static Place MapToPlace(this PlaceDTO dto)
        {
            return new Place
            {
                Name = dto.Name,
                Type = dto.Type,
                Indoor = dto.Indoor,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Capacity = dto.Capacity,
                Tag = dto.Tag
            };
        }
        public static PlaceDTO MapToPlaceWithLatitude(this PlaceDTO dto)
        {
            return new PlaceDTO
            {
                Name = dto.Name,
                Type = dto.Type,
                Indoor = dto.Indoor,
                Latitude = Math.Round(dto.Latitude, 2),
                Longitude = Math.Round(dto.Longitude, 3),
                Capacity = dto.Capacity,
                Tag = dto.Tag
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
        //DTO → Suggestion
        public static Suggestion MapToSuggestion(this SuggestionDTO dto)
        {
            return new Suggestion
            {
                User_Id = dto.UserId,
                DateSuggestion = dto.DateSuggestion,
                OriginalPlace = dto.OriginalPlace,
                SuggestedAlternatives = dto.SuggestedAlternatives,
                Reason = dto.Reason
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
        // DTO side standardization (default date + truncation)
        public static TrafficConditionDTO Normalize(this TrafficConditionDTO dto)
            => new TrafficConditionDTO
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition == default ? DateTime.UtcNow
                                                                  : TruncateToSecond(dto.DateCondition),
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType
            };

        // DTO -> Entity (CREATE)
        public static TrafficCondition MapToTrafficCondition(this TrafficConditionDTO dto)
        => new TrafficCondition
        {
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            DateCondition = dto.DateCondition,
            CongestionLevel = dto.CongestionLevel,
            IncidentType = dto.IncidentType,
        };

        // DTO -> Entity (UPDATE) existe déjà pour TrafficConditionUpdateDTO → OK

        public static TrafficConditionDTO MapToTrafficConditionWithDateCondition(this TrafficConditionUpdateDTO dto)
        {
            return new TrafficConditionDTO
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType
            };
        }
        // ✅ Create : TrafficConditionDTO -> Entity
        public static TrafficCondition MapToEntity(this TrafficConditionDTO dto)
        {
            if (dto == null) return null!;

            return new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition == default ? DateTime.UtcNow : dto.DateCondition,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType
            };
        }
        // ✅ Update: already exists for UpdateDTO
        public static TrafficCondition MapToEntity(this TrafficConditionUpdateDTO dto)
        {
            if (dto == null) return null!;

            var entity = new TrafficCondition
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition,
                CongestionLevel = dto.CongestionLevel,
                IncidentType = dto.IncidentType
            };
            return entity.WithId(dto.Id); // helper domain if you have it, otherwise pass the Id to the repo
        }

        //Mapping to DTO
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