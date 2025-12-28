namespace CitizenHackathon2025.Infrastructure.Options
{
    public sealed class TrafficOptions
    {
        public int CollectorPeriodSeconds { get; set; } = 60;

        public string[] Providers { get; set; } = [];

        public TrafficBBoxOptions BBox { get; set; } = new();

        public sealed class TrafficBBoxOptions
        {
            public decimal MinLat { get; set; }
            public decimal MinLon { get; set; }
            public decimal MaxLat { get; set; }
            public decimal MaxLon { get; set; }
        }
    }
}
