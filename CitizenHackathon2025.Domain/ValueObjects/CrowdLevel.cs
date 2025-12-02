using System;
using CitizenHackathon2025.Contracts.Enums;

namespace CitizenHackathon2025.Domain.ValueObjects
{
    /// <summary>
    /// Represents the crowding density in a given area,
    /// encapsulates the CrowdLevelEnum with business logic.
    /// </summary>
    public sealed class CrowdLevel : IEquatable<CrowdLevel>
    {
        public CrowdLevelEnum Level { get; }

        public CrowdLevel(CrowdLevelEnum level)
        {
            Level = level;
        }

        /// <summary>
        /// Calculates a crowd level based on observed density (people/m²).
        /// </summary>
        public static CrowdLevel FromDensity(double peoplePerSquareMeter)
        {
            if (peoplePerSquareMeter < 1.0)
                return new CrowdLevel(CrowdLevelEnum.Low);

            if (peoplePerSquareMeter < 2.5)
                return new CrowdLevel(CrowdLevelEnum.Medium);

            if (peoplePerSquareMeter < 4.0)
                return new CrowdLevel(CrowdLevelEnum.High);

            return new CrowdLevel(CrowdLevelEnum.Critical);
        }

        public override string ToString() => Level.ToString();

        public bool Equals(CrowdLevel? other)
            => other is not null && Level == other.Level;

        public override bool Equals(object? obj)
            => obj is CrowdLevel cl && Equals(cl);

        public override int GetHashCode() => Level.GetHashCode();
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.