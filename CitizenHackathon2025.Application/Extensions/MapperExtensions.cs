using CitizenHackathon2025.Application.Extensions;
using CitizenHackathon2025.Contracts.Enums;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.ValueObjects;
using CitizenHackathon2025.DTOs.DTOs;
using Volo.Abp.Domain.Entities;

namespace CitizenHackathon2025.Application.Extensions
{
    public static class MapperExtensions
    {
    #nullable disable
        private static decimal RoundLat(double lat) => Math.Round((decimal)lat, 6);
        private static decimal RoundLon(double lon) => Math.Round((decimal)lon, 6);
        private static DateTime TruncateToSecond(DateTime dt)
        {
            var ticks = dt.Ticks - (dt.Ticks % TimeSpan.TicksPerSecond);
            return new DateTime(ticks, dt.Kind);
        }
        private static DateTimeOffset TruncateToSecond(DateTimeOffset dto)
        {
            var ticks = dto.UtcTicks - (dto.UtcTicks % TimeSpan.TicksPerSecond);
            return new DateTimeOffset(ticks, TimeSpan.Zero); // standardized UTC
        }


        // DTO -> Entity (WeatherForecastDTO -> Domain.Entities.WeatherForecast)
        public static WeatherForecast MapToWeatherForecast(this WeatherForecastDTO dto)
        {
            return new WeatherForecast
            {
                Id = dto.Id,
                DateWeatherUtc = dto.DateWeather.UtcDateTime, // ✅ UTC normalization
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TemperatureC = dto.TemperatureC,
                Humidity = dto.Humidity,
                WindSpeedKmh = dto.WindSpeedKmh,
                RainfallMm = dto.RainfallMm,
                Summary = dto.Summary,
                WeatherMain = dto.WeatherMain,
                Description = dto.Description,
                Icon = dto.Icon,
                IconUrl = dto.IconUrl,
                IsSevere = dto.IsSevere,
                WeatherType = dto.WeatherType
            };
        }

        // Entity -> DTO (Domain.Entities.WeatherForecast -> WeatherForecastDTO)
        public static WeatherForecastDTO MapToWeatherForecastDTO(this WeatherForecast entity)
        {
            if (entity is null) return null!;

            var lat = entity.Latitude ?? 50.89m;
            var lon = entity.Longitude ?? 4.34m;

            // UTC normalization + second
            var dtUtc = entity.DateWeatherUtc.Kind == DateTimeKind.Utc
                ? entity.DateWeatherUtc
                : DateTime.SpecifyKind(entity.DateWeatherUtc, DateTimeKind.Utc);

            dtUtc = new DateTime(dtUtc.Ticks - (dtUtc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

            return new WeatherForecastDTO
            {
                Id = entity.Id,
                DateWeather = new DateTimeOffset(dtUtc, TimeSpan.Zero),

                Latitude = lat,
                Longitude = lon,

                TemperatureC = entity.TemperatureC,
                Humidity = entity.Humidity,
                WindSpeedKmh = entity.WindSpeedKmh,
                RainfallMm = entity.RainfallMm,

                Summary = entity.Summary ?? string.Empty,
                WeatherMain = entity.WeatherMain ?? string.Empty,
                Description = entity.Description,

                Icon = entity.Icon,
                IconUrl = entity.IconUrl ?? string.Empty,

                IsSevere = entity.IsSevere,
                WeatherType = entity.WeatherType
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
                RetrievedAt = dto.DateWeather.UtcDateTime
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
               PlaceId = entity.PlaceId,
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
                 PlaceId = dto.PlaceId,
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
                PlaceId = dto.PlaceId,
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
                Active = entity.Active,

                // Map context not stored in database :
                EventId = null,
                CrowdInfoId = null,
                PlaceId = null,
                TrafficConditionId = null,
                WeatherForecastId = null,
                Latitude = null,
                Longitude = null,
                SourceType = null,
                CrowdLevel = null
            };
        }

        public static GPTInteraction MapToGptInteraction(this GptInteractionDTO dto)
        {
            if (dto is null) return null!;

            return new GPTInteraction
            {
                Id = dto.Id,
                Prompt = dto.Prompt,
                Response = dto.Response,
                PromptHash = string.IsNullOrWhiteSpace(dto.PromptHash) ? null! : dto.PromptHash,
                CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                Active = dto.Active
                // Context fields are NOT mapped to the entity
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
        // Entity -> DTO (UserMessage -> ClientMessageDTO)
        public static ClientMessageDTO MapToClientMessageDTO(this UserMessage m)
        {
            if (m is null) return null!;

            return new ClientMessageDTO
            {
                Id = m.Id,
                UserId = m.UserId,
                SourceType = m.SourceType,
                SourceId = m.SourceId,
                RelatedName = m.RelatedName,
                Latitude = m.Latitude.HasValue ? (double?)m.Latitude.Value : null,
                Longitude = m.Longitude.HasValue ? (double?)m.Longitude.Value : null,
                Tags = m.Tags,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            };
        }

        // Optional: collection helper
        public static List<ClientMessageDTO> MapToClientMessageDTOs(this IEnumerable<UserMessage> items)
            => items?.Select(x => x.MapToClientMessageDTO()).ToList() ?? new List<ClientMessageDTO>();
    
        // Place → DTOSugges
        public static PlaceDTO MapToPlaceDTO(this Place entity)
        {
            return new PlaceDTO
            {
                Id = entity.Id,
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
                Id = dto.Id,
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
                Id = dto.Id,
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
        public static SuggestionDTO MapToSuggestionDTO(this Suggestion row)
        {
            if (row is null) return null!;
            return new SuggestionDTO
            {
                Id = row.Id,
                UserId = row.User_Id,
                DateSuggestion = row.DateSuggestion,
                OriginalPlace = row.OriginalPlace,
                SuggestedAlternatives = row.SuggestedAlternatives,
                Reason = row.Reason ?? "",
                Active = row.Active,

                EventId = row.EventId,
                PlaceId = row.PlaceId,

                Latitude = row.Latitude,
                Longitude = row.Longitude,
                LocationLabel = row.LocationLabel,

                // what you want to display on the map / popup
                Title = row.LocationLabel ?? row.OriginalPlace ?? row.LocationName
            };
        }

        //DTO → Suggestion
        public static Suggestion MapToSuggestion(this SuggestionDTO dto)
        {
            if (dto is null) return null!;

            return new Suggestion
            {
                Id = dto.Id,
                User_Id = dto.UserId,
                DateSuggestion = dto.DateSuggestion,
                OriginalPlace = dto.OriginalPlace,
                SuggestedAlternatives = dto.SuggestedAlternatives,
                Reason = dto.Reason,
                Active = dto.Active,

                Message = dto.Message ?? "",
                Context = dto.Context ?? "",

                EventId = dto.EventId,
                PlaceId = dto.PlaceId,

                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DistanceKm = dto.DistanceKm,
                LocationLabel = dto.LocationLabel,

                LocationName = dto.LocationLabel ?? dto.Title ?? dto.OriginalPlace
                // si tu as Title en DB : ajoute Title dans l'entity aussi
            };
        }


        // TrafficCondition → DTO
        public static TrafficConditionDTO MapToTrafficConditionDTO(this TrafficCondition entity)
        {
            return new TrafficConditionDTO
            {
                Id = entity.Id,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                DateCondition = entity.DateCondition,
                CongestionLevel = entity.CongestionLevel,
                IncidentType = entity.IncidentType,

                Location = entity.Road ?? "",
                Level = entity.Severity,
                Message = entity.Title ?? ""
            };
        }
        // DTO side standardization (default date + truncation)
        public static TrafficConditionDTO Normalize(this TrafficConditionDTO dto)
            => new TrafficConditionDTO
            {
                Id = dto.Id,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateCondition = dto.DateCondition == default ? DateTime.UtcNow : TruncateToSecond(dto.DateCondition),
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

                Road = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location,
                Severity = dto.Level,
                Title = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message,

                Provider = "manual",
                ExternalId = $"manual-{Guid.NewGuid():N}",
                Fingerprint = System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes($"manual|{dto.Latitude}|{dto.Longitude}|{dto.DateCondition:O}|{dto.IncidentType}")
                ),
                LastSeenAt = DateTime.UtcNow
            };


        // DTO -> Entity (UPDATE) existe déjà pour TrafficConditionUpdateDTO → OK

        public static TrafficConditionDTO MapToTrafficConditionWithDateCondition(this TrafficConditionUpdateDTO dto)
        {
            return new TrafficConditionDTO
            {
                Id = dto.Id,
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
        // ============================
        // CrowdInfoAntenna mappings
        // ============================

        public static CrowdInfoAntennaDTO MapToCrowdInfoAntennaDTO(this CrowdInfoAntenna entity)
        {
            if (entity is null) return null!;

            return new CrowdInfoAntennaDTO
            {
                Id = entity.Id,
                Name = entity.Name ?? string.Empty,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                CreatedUtc = entity.CreatedUtc,
                Description = entity.Description,
                MaxCapacity = entity.MaxCapacity
            };
        }

        public static CrowdInfoAntenna MapToCrowdInfoAntenna(this CreateCrowdInfoAntennaDTO dto)
        {
            if (dto is null) return null!;

            return new CrowdInfoAntenna
            {
                Name = dto.Name?.Trim() ?? string.Empty,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Description = dto.Description?.Trim(),
                MaxCapacity = dto.MaxCapacity,
                Active = true
            };
        }
    }     
}





















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.