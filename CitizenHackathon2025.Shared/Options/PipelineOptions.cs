namespace CitizenHackathon2025.Shared.Options
{
    public sealed class PipelineOptions
    {
        public WeatherPipelineOptions WeatherForecast { get; set; } = new();
        public TrafficPipelineOptions TrafficCondition { get; set; } = new();
        public CrowdAntennaPipelineOptions CrowdInfoAntenna { get; set; } = new();
        public WepPipelineOptions WallonieEnPoche { get; set; } = new();
        public ArchiverPipelineOptions Archiver { get; set; } = new();
    }
    public sealed class WeatherPipelineOptions
    {
        public bool Enabled { get; set; } = true;
        public int PeriodSeconds { get; set; } = 60;
        public decimal DefaultLatitude { get; set; } = 50.412381m;
        public decimal DefaultLongitude { get; set; } = 4.320844m;
    }

    public sealed class TrafficPipelineOptions
    {
        public bool Enabled { get; set; } = true;
        public int PeriodSeconds { get; set; } = 60;
        public int Limit { get; set; } = 50;
    }

    public sealed class CrowdAntennaPipelineOptions
    {
        public bool Enabled { get; set; } = true;
        public int PeriodSeconds { get; set; } = 10;
        public int WindowMinutes { get; set; } = 10;
    }

    public sealed class WepPipelineOptions
    {
        public bool Enabled { get; set; } = true;
        public int PeriodSeconds { get; set; } = 300;
    }

    public sealed class ArchiverPipelineOptions
    {
        public bool Enabled { get; set; } = true;
        public int PeriodSeconds { get; set; } = 60;
    }
}




















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.