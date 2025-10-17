namespace CitizenHackathon2025.Infrastructure.ExternalAPIs.OpenWeather
{
    public sealed class OpenWeatherResponse
    {
        public Coord coord { get; set; } = default!;
        public Main main { get; set; } = default!;
        public Wind wind { get; set; } = default!;
        public Weather[] weather { get; set; } = Array.Empty<Weather>();
        public long dt { get; set; }
    }

    public sealed class Coord { public decimal lon { get; set; } public decimal lat { get; set; } }
    public sealed class Main { public double temp { get; set; } public int humidity { get; set; } }
    public sealed class Wind { public double speed { get; set; } }
    public sealed class Weather { public string description { get; set; } = ""; }
}
















































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.