
//using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Services;

namespace CitizenHackathon2025.Tests.Domain.Services;

public class WeatherForecastDomainServiceTests
{
    private readonly WeatherForecastDomainService _service = new();

    //    [Fact]
    //    public void IsWeatherSuitableForOutdoorActivity_ValidTemperatureAndNoRain_ReturnsTrue()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = 20,
    //            Summary = "Sunny"
    //        };

    //        Assert.True(_service.IsWeatherSuitableForOutdoorActivity(forecast));
    //    }

    //    [Fact]
    //    public void IsWeatherSuitableForOutdoorActivity_TooCold_ReturnsFalse()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = -5,
    //            Summary = "Clear"
    //        };

    //        Assert.False(_service.IsWeatherSuitableForOutdoorActivity(forecast));
    //    }

    //    [Fact]
    //    public void IsWeatherSuitableForOutdoorActivity_WithRain_ReturnsFalse()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = 25,
    //            Summary = "Heavy rain showers"
    //        };

    //        Assert.False(_service.IsWeatherSuitableForOutdoorActivity(forecast));
    //    }

    //    [Theory]
    //    [InlineData(22, "Clear", 35)]
    //    [InlineData(30, "Humid", 17)]
    //    public void CalculateComfortIndex_ReturnsExpectedValue(int temp, string summary, double expected)
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = temp,
    //            Summary = summary
    //        };

    //        var actual = _service.CalculateComfortIndex(forecast);
    //        Assert.Equal(expected, actual);
    //    }

    //    [Fact]
    //    public void IsForecastValid_ValidForecast_ReturnsTrue()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = 15,
    //            Summary = "Cloudy"
    //        };

    //        Assert.True(_service.IsForecastValid(forecast));
    //    }

    //    [Fact]
    //    public void IsForecastValid_InvalidSummary_ReturnsFalse()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = 15,
    //            Summary = "  "
    //        };

    //        Assert.False(_service.IsForecastValid(forecast));
    //    }

    //    [Fact]
    //    public void IsForecastValid_TemperatureTooHigh_ReturnsFalse()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today,
    //            TemperatureC = 70,
    //            Summary = "Hot"
    //        };

    //        Assert.False(_service.IsForecastValid(forecast));
    //    }

    //    [Fact]
    //    public void IsForecastValid_DateInPast_ReturnsFalse()
    //    {
    //        var forecast = new WeatherForecast
    //        {
    //            DateWeather = DateTime.Today.AddDays(-1),
    //            TemperatureC = 15,
    //            Summary = "Nice"
    //        };

    //        Assert.False(_service.IsForecastValid(forecast));
}


































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.