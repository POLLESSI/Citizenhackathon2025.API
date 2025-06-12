﻿using System;

namespace Citizenhackathon2025.Domain.Entities.ValueObjects
{
    /// <summary>
    /// Represents a geolocation by latitude and longitude.
    /// </summary>
    public record Location(double Latitude, double Longitude)
    {
        public override string ToString() => $"({Latitude}, {Longitude})";

        public static Location Create(double latitude, double longitude)
        {
            if (latitude is < -90 or > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

            if (longitude is < -180 or > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

            return new Location(latitude, longitude);
        }
    }
}
