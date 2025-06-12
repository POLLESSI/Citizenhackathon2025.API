using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class WeatherForecastDTO
    {
    #nullable disable
        public int Id { get; set; }
        [DisplayName("Weather Date : ")]
        public DateTime DateWeather { get; set; }
        [JsonIgnore] // On ignore ce champ à l'entrée (POST), il est calculé automatiquement
        public string DateWeatherFormatted => DateWeather.ToString("dd/MM/yyyy");
        [JsonIgnore]
        public string DayOfWeek => DateWeather.ToString("dddd"); // Exemple : "Saturday"
        [JsonIgnore]
        public string MonthName => DateWeather.ToString("MMMM"); // Exemple : "April"
        [DisplayName("Temperature C : ")]
        public string TemperatureC { get; set; }
        [DisplayName("Temperature F : ")]
        public string TemperatureF => 32 + (string)("TemperatureC / 0.5556");
        [DisplayName("Summary : ")]
        public string Summary { get; set; } = "";
        [DisplayName("Rainfall mm : ")]
        public string RainfallMm { get; set; }
        [DisplayName("Humidity : ")]
        public string Humidity { get; set; }
        [DisplayName("Wind Speed km/h : ")]
        public string WindSpeedKmh { get; set; }

        //public static implicit operator WeatherForecastDTO(WeatherForecastDTO v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
