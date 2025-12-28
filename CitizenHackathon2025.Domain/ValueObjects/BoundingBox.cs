namespace CitizenHackathon2025.Domain.ValueObjects
{
    public sealed record BoundingBox(
    decimal MinLat,
    decimal MinLon,
    decimal MaxLat,
    decimal MaxLon)
    {
        public void Deconstruct(out decimal minLat, out decimal minLon, out decimal maxLat, out decimal maxLon)
            => (minLat, minLon, maxLat, maxLon) = (MinLat, MinLon, MaxLat, MaxLon);

        public bool IsValid()
            => MinLat <= MaxLat && MinLon <= MaxLon;
    }
}
