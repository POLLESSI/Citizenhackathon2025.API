namespace CitizenHackathon2025.Shared.Options
{
    public sealed class OpenWeatherOptions
    {
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "https://api.openweathermap.org";
    }
}
