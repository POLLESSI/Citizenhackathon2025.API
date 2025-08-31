using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace CitizenHackathon2025.DTOs.DTOs
{
    public class WeatherForecastDTO
    {
    #nullable disable
        public int Id { get; set; }
        public DateTime DateWeather { get; set; }

        [DisplayName("Temperature C : ")]
        public int TemperatureC { get; set; }

        [DisplayName("Temperature F : ")]
        [JsonIgnore]
        public string TemperatureF
        {
            get
            {
                var tempF = 32 + (int)(TemperatureC / 0.5556);
                return tempF.ToString(CultureInfo.InvariantCulture);
            }
        }

        [DisplayName("Summary : ")]
        public string Summary { get; set; } = "";

        [DisplayName("Rainfall mm : ")]
        public double RainfallMm { get; set; }

        [DisplayName("Humidity : ")]
        public int Humidity { get; set; }

        [DisplayName("Wind Speed km/h : ")]
        public double WindSpeedKmh { get; set; }
        [DisplayName("Icon : ")]
        public string? Icon { get; set; }

        [JsonIgnore]
        public string DateWeatherFormatted => DateWeather.ToString("dd/MM/yyyy");

        [JsonIgnore]
        public string DayOfWeek => DateWeather.ToString("dddd", CultureInfo.InvariantCulture);

        [JsonIgnore]
        public string MonthName => DateWeather.ToString("MMMM", CultureInfo.InvariantCulture);

        public string IconUrl { get; set; } = ""; // ex: https://openweathermap.org/img/wn/{icon}.png
        public string WeatherMain { get; set; } = "";
        public bool IsSevere { get; set; }
        public string Description { get; set; }
    }
}

















































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.