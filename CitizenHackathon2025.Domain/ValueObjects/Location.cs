namespace CitizenHackathon2025.Domain.ValueObjects
{
    public sealed class Location : IEquatable<Location>
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public Location(double latitude, double longitude)
        {
            if (latitude is < -90 or > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
            if (longitude is < -180 or > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

            Latitude = latitude;
            Longitude = longitude;
        }

        public override bool Equals(object? obj) => Equals(obj as Location);

        public bool Equals(Location? other) =>
            other is not null &&
            Latitude.Equals(other.Latitude) &&
            Longitude.Equals(other.Longitude);

        public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

        public static bool operator ==(Location? left, Location? right) => Equals(left, right);
        public static bool operator !=(Location? left, Location? right) => !Equals(left, right);

        public override string ToString() => $"({Latitude}, {Longitude})";
    }
}
