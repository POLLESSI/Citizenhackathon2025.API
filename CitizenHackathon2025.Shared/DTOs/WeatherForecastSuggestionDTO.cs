using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Citizenhackathon2025.Shared.DTOs
{
    public class WeatherForecastSuggestionDTO
    {
#nullable disable
        public string TemperatureC { get; set; }
        public string Humidity { get; set; }
        public string Location
        {
            get; set;
        }
    }
}
