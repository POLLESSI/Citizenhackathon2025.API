using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<WeatherForecastRepository> _logger;
        private readonly Random _rng = new();

        public WeatherForecastRepository(IDbConnection connection, ILogger<WeatherForecastRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public Task<WeatherForecast?> GetLatestWeatherForecastAsync(CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(1)
                                Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM dbo.WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";

            return _connection.QueryFirstOrDefaultAsync<WeatherForecast>(
                new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<List<WeatherForecast>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT
                                Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM dbo.WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";

            var rows = await _connection.QueryAsync<WeatherForecast>(
                new CommandDefinition(sql, cancellationToken: ct));

            return rows.ToList();
        }

        public async Task<List<WeatherForecast>> GetHistoryAsync(int limit = 128, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(@Limit)
                                Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM dbo.WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";

            var rows = await _connection.QueryAsync<WeatherForecast>(
                new CommandDefinition(sql, new { Limit = limit }, cancellationToken: ct));

            return rows.ToList();
        }

        public Task<WeatherForecast?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT
                                Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM dbo.WeatherForecast
                            WHERE Id = @Id AND Active = 1;";

            return _connection.QueryFirstOrDefaultAsync<WeatherForecast>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        }

        public async Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity, CancellationToken ct = default)
        {
            const string sql = @"EXEC dbo.sp_WeatherForecast_Upsert
                            @DateWeather, @Latitude, @Longitude, @TemperatureC, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh;";

            var args = new
            {
                entity.DateWeather,
                entity.Latitude,
                entity.Longitude,
                entity.TemperatureC,
                entity.Summary,
                entity.RainfallMm,
                entity.Humidity,
                entity.WindSpeedKmh
            };

            // WHY: QuerySingleAsync + ct => annulation propre si le client coupe
            return await _connection.QuerySingleAsync<WeatherForecast>(
                new CommandDefinition(sql, args, cancellationToken: ct));
        }

        public async Task<WeatherForecast> GenerateNewForecastAsync(CancellationToken ct = default)
        {
            decimal lat = 50.2m + (decimal)_rng.NextDouble() * 0.7m;
            decimal lon = 4.0m + (decimal)_rng.NextDouble() * 1.1m;

            var wf = new WeatherForecast
            {
                DateWeather = DateTime.UtcNow,
                Latitude = lat,
                Longitude = lon,
                TemperatureC = _rng.Next(-10, 35),
                Summary = "Generated",
                RainfallMm = Math.Round(_rng.NextDouble() * 20, 1),
                Humidity = _rng.Next(30, 100),
                WindSpeedKmh = Math.Round(_rng.NextDouble() * 80, 1)
            };

            return await SaveOrUpdateAsync(wf, ct);
        }

        public async Task<int> ArchivePastWeatherForecastsAsync(CancellationToken ct = default)
        {
            const string sql = @"
                            UPDATE dbo.WeatherForecast
                            SET Active = 0
                            WHERE Active = 1
                              AND DateWeather < DATEADD(DAY, -1, CAST(GETDATE() AS DATETIME2(0)));";

            try
            {
                var affected = await _connection.ExecuteAsync(
                    new CommandDefinition(sql, cancellationToken: ct));

                _logger.LogInformation("{Count} Weather Forecast(s) archived.", affected);
                return affected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving past Weather Forecasts.");
                return 0;
            }
        }

        public async Task<(double LastHour, double Last72h)> GetRainAccumulationAsync(
            decimal latitude,
            decimal longitude,
            DateTime asOfUtc,
            CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT
                                SUM(CASE WHEN DateWeather >= DATEADD(HOUR, -1,  @Now) THEN ISNULL(RainfallMm, 0) ELSE 0 END) AS LastHour,
                                SUM(CASE WHEN DateWeather >= DATEADD(HOUR, -72, @Now) THEN ISNULL(RainfallMm, 0) ELSE 0 END) AS Last72h
                            FROM dbo.WeatherForecast
                            WHERE Active = 1
                              AND ABS(Latitude  - @Lat) <= @Delta
                              AND ABS(Longitude - @Lon) <= @Delta;";

            var args = new
            {
                Now = asOfUtc,
                Lat = latitude,
                Lon = longitude,
                Delta = 0.05m // WHY: “proximité” ~ 5km (approx)
            };

            var result = await _connection.QuerySingleAsync<(double LastHour, double Last72h)>(
                new CommandDefinition(sql, args, cancellationToken: ct));

            return result;
        }
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.