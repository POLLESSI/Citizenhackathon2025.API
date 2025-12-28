namespace CitizenHackathon2025.Domain.ValueObjects
{
    public static class SensitiveFloodAreas
    {
        // Approx : Caves of Han
        public static readonly (string Name, double Lat, double Lon, double RadiusKm)[] All =
        {
        ("Grottes de Han", 50.126, 5.185, 5.0)
        // You can add as many as you want
    };

        public static (bool IsNear, string? Name) Check(double lat, double lon)
        {
            foreach (var (name, alat, alon, radiusKm) in All)
            {
                var d = HaversineDistanceKm(lat, lon, alat, alon);
                if (d <= radiusKm)
                    return (true, name);
            }
            return (false, null);
        }

        private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            lat1 *= Math.PI / 180.0;
            lat2 *= Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }

}




























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.