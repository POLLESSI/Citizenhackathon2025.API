using GeoAPI.Geometries;
using System.Text.Json;

namespace CitizenHackathon2025.API.Services
{
    public static class EmergencyAlertGeoJsonSerializer
    {
        public static string? Serialize(NetTopologySuite.Geometries.Geometry? geometry)
        {
            if (geometry is null)
                return null;


            object? geoJson = null;


            if (geometry is IPoint point)
            {
                geoJson = new
                {
                    type = "Point",

                    coordinates = new[]
                    {
                        point.X,
                        point.Y
                    }
                };
            }
            else if (geometry is IPolygon polygon)
            {
                geoJson = new
                {
                    type = "Polygon",

                    coordinates =
                        ToPolygonCoordinates(
                            polygon)
                };
            }
            else if (geometry is IMultiPolygon multiPolygon)
            {
                geoJson = new
                {
                    type = "MultiPolygon",

                    coordinates =
                        ToMultiPolygonCoordinates(
                            multiPolygon)
                };
            }


            return geoJson is null
                ? null
                : JsonSerializer.Serialize(
                    geoJson);
        }


        private static double[][][] ToPolygonCoordinates(IPolygon polygon)
        {
            var rings =
                new List<double[][]>(
                    1 +
                    polygon.NumInteriorRings);


            /*
             * Exterior ring.
             *
             * GeoJSON coordinates are:
             *
             * X = longitude
             * Y = latitude
             */
            rings.Add(
                polygon
                    .ExteriorRing
                    .Coordinates
                    .Select(
                        c => new[]
                        {
                            c.X,
                            c.Y
                        })
                    .ToArray());


            /*
             * Holes.
             */
            for (
                var i = 0;
                i < polygon.NumInteriorRings;
                i++)
            {
                rings.Add(
                    polygon
                        .GetInteriorRingN(i)
                        .Coordinates
                        .Select(
                            c => new[]
                            {
                                c.X,
                                c.Y
                            })
                        .ToArray());
            }


            return rings.ToArray();
        }


        private static double[][][][] ToMultiPolygonCoordinates(IMultiPolygon multiPolygon)
        {
            var polygons =
                new List<double[][][]>(
                    multiPolygon.NumGeometries);


            for (
                var i = 0;
                i < multiPolygon.NumGeometries;
                i++)
            {
                if (
                    multiPolygon
                        .GetGeometryN(i)
                    is not IPolygon polygon)
                {
                    continue;
                }


                polygons.Add(
                    ToPolygonCoordinates(
                        polygon));
            }


            return polygons.ToArray();
        }
    }
}









































































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.