using System;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Shared.DTOs;

namespace CitizenHackathon2025.Shared.Extentions
{
    public static class TrafficConditionDTOExtensions
    {
        public static TrafficCondition? ToNumeric(this TrafficConditionDTO dto)
        {
            if (dto == null) return null;

            try
            {
                // String → decimal conversion (optional depending on your current class)
                if (!decimal.TryParse(dto.Latitude, out var latitude))
                    throw new FormatException("Invalid latitude");
                if (!decimal.TryParse(dto.Longitude, out var longitude))
                    throw new FormatException("Invalid longitude");

                return new TrafficCondition
                {
                    Latitude = latitude.ToString("F2"), // preserve decimal formatting
                    Longitude = longitude.ToString("F3"),
                    DateCondition = dto.DateCondition,
                    CongestionLevel = dto.CongestionLevel,
                    IncidentType = dto.IncidentType
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToNumeric (TrafficCondition) : {ex.Message}");
                return null;
            }
        }
    }
}
































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.