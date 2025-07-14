using System;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.DTOs.DTOs;


namespace CitizenHackathon2025.Shared.Extentions
{
    public static class CrowdInfoDTOExtensions
    {
        public static CrowdInfo? ToNumeric(this CrowdInfoDTO dto)
        {
            if (dto == null) return null;

            try
            {
                // Optional verification and parsing if needed
                if (!decimal.TryParse(dto.Latitude, out var lat))
                    throw new FormatException("Latitude invalide");
                if (!decimal.TryParse(dto.Longitude, out var lon))
                    throw new FormatException("Longitude invalide");
                if (!int.TryParse(dto.CrowdLevel, out var level))
                    throw new FormatException("CrowdLevel invalide");

                return new CrowdInfo
                {
                    LocationName = dto.LocationName,
                    Latitude = lat.ToString("F6"),
                    Longitude = lon.ToString("F6"),
                    CrowdLevel = level.ToString(),
                    Timestamp = dto.Timestamp,
                    Active = true // Default value (can be implicit)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans ToNumeric (CrowdInfo) : {ex.Message}");
                return null;
            }
        }
    }
}







































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.