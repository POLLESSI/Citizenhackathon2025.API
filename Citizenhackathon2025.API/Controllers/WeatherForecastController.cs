using CitizeHackathon2025.Hubs.Hubs;
using Citizenhackathon2025.Application.WeatherForecast.Queries;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Citizenhackathon2025.Shared.DTOs;
using CitizenHackathon2025.Application.Interfaces;
using CitizenHackathon2025.Application.Services;
using CityzenHackathon2025.API.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Numerics;

namespace Citizenhackathon2025.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly IWeatherForecastRepository _weatherRepository;
        private readonly IHubContext<WeatherForecastHub> _hubContext;
        private readonly IOpenWeatherMapService _owmService;
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMediator _mediator;

        public WeatherForecastController(IWeatherForecastRepository weatherRepository, IHubContext<WeatherForecastHub> hubContext, IOpenWeatherMapService owmService, IMediator mediator)
        {
            _weatherRepository = weatherRepository;
            _hubContext = hubContext;
            _owmService = owmService;
            _mediator = mediator;
        }
        //[HttpGet("openweather")]
        //public async Task<IActionResult> GetForecastFromOpenWeather()
        //{
        //    var externalDto = await _owmService.GetForecastAsync("Namur");
        //    if (externalDto == null)
        //        return NotFound();

        //    // DTO → Entity → DTO (unification de mapping)
        //    var entity = externalDto.MapToWeatherForecast();
        //    var apiDto = entity.MapToWeatherForecastDTO();

        //    await _hubContext.Clients.All.SendAsync("ExternalWeatherUpdate", apiDto);
        //    return Ok(apiDto);
        //}
        [HttpGet("current")]
        public async Task<ActionResult<WeatherForecast>> GetCurrentWeather()
        {
            var forecast = await _weatherRepository.GenerateNewForecastAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveWeather", forecast);
            return Ok(forecast);
        }
        [HttpGet("history")]
        public async Task<ActionResult<List<WeatherForecast>>> GetHistory()
        {
            return Ok(await _weatherRepository.GetHistoryAsync());
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<Domain.Entities.WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new Domain.Entities.WeatherForecast
            {
                DateWeather = DateTime.UtcNow,
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestWeather()
        {
            var result = await _mediator.Send(new GetLatestForecastQuery());
            return result != null ? Ok(result) : NotFound();
        }
        //[HttpPost]
        //public async Task<IActionResult> SaveWeatherForecast([FromBody] WeatherForecastDTO forecastDto)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var forecast = forecastDto.MapToWeatherForecast();

        //    var savedForecast = await _weatherRepository.SaveWeatherForecastAsync(forecast);

        //    if (savedForecast == null)
        //        return StatusCode(500, "Registration Error");

        //    var forecastDtoToSend = savedForecast.MapToWeatherForecastDTO();

        //    await _hubContext.Clients.All.SendAsync("NewWeatherForecast", forecastDtoToSend);

        //    return Ok(forecastDtoToSend);
        //}
    }
}

