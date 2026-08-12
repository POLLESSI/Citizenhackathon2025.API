using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System.Globalization;

namespace CitizenHackathon2025.EmergencyIntelligence.Sources.BeAlert
{
    internal static class BeAlertCapGeometryParser
    {
        public static IPolygon? ParsePolygon(string raw, GeometryFactory geometryFactory)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var coordinates = raw
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseCoordinate)
                .Where(x => x is not null)
                .Cast<Coordinate>()
                .ToArray();

            if (coordinates.Length < 4)
                return null;

            /*
             * CAP polygons should form a closed ring.
             * Close it ourselves if necessary.
             */
            if (!coordinates[0].Equals2D(coordinates[^1]))
            {
                coordinates = [.. coordinates, coordinates[0]];
            }

            try
            {
                return geometryFactory.CreatePolygon(coordinates);
            }
            catch (ArgumentException)
            {
                /*
                 * A malformed external CAP polygon
                 * must not crash the complete
                 * emergency synchronization.
                 */
                return null;
            }
        }

        private static Coordinate? ParseCoordinate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var parts = value.Split(',', StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                return null;

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            {
                return null;
            }

            /*
             * CAP = latitude,longitude
             * NTS = X,Y = longitude,latitude
             */
            return new Coordinate(longitude, latitude);
        }
    }
}

























































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.