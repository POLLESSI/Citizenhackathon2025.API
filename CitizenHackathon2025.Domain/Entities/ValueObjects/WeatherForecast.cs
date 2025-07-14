using Volo.Abp.Domain.Entities;

namespace CitizenHackathon2025.Domain.Entities.ValueObjects
{
    public class WeatherForecast : AggregateRoot<Guid>
    {
        
        public Location Location { get; private set; }
        public DateTime DateWeather { get; private set; }
        public int TemperatureC { get; private set; }

        public WeatherForecast(Location location, DateTime dateWeather, int temperatureC)
        {
            Location = location;
            DateWeather = dateWeather;
            TemperatureC = temperatureC;
        }

        public static WeatherForecast Create(Location location, DateTime dateWeather, int temperatureC)
        {
            if (temperatureC < -100 || temperatureC > 100)
                throw new ArgumentOutOfRangeException(nameof(temperatureC), "Invalid temperature");

            return new WeatherForecast(location, dateWeather, temperatureC);
        }
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.