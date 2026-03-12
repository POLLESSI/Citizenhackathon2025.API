using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.ReadRows;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

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
        public async Task<WeatherForecast?> GetLatestWeatherForecastAsync(CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT TOP(1)
                                Id,
                                DateWeather AS DateWeatherUtc,
                                Latitude,
                                Longitude,
                                TemperatureC,
                                TemperatureF,
                                Summary,
                                RainfallMm,
                                Humidity,
                                WindSpeedKmh,
                                Active
                            FROM dbo.WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;
                            ";

            return await _connection.QueryFirstOrDefaultAsync<WeatherForecast>(sql);
        }
        
        public async Task<List<WeatherForecast>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = @"
                            SELECT
                                Id,
                                DateWeather AS DateWeatherUtc,
                                Latitude,
                                Longitude,
                                TemperatureC,
                                TemperatureF,
                                Summary,
                                RainfallMm,
                                Humidity,
                                WindSpeedKmh,
                                Active
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
                                Id,
                                DateWeather AS DateWeatherUtc,
                                Latitude,
                                Longitude,
                                TemperatureC,
                                TemperatureF,
                                Summary,
                                RainfallMm,
                                Humidity,
                                WindSpeedKmh,
                                Active
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
                                Id,
                                DateWeather AS DateWeatherUtc,
                                Latitude,
                                Longitude,
                                TemperatureC,
                                TemperatureF,
                                Summary,
                                RainfallMm,
                                Humidity,
                                WindSpeedKmh,
                                Active
                            FROM dbo.WeatherForecast
                            WHERE Id = @Id AND Active = 1;";

            return _connection.QueryFirstOrDefaultAsync<WeatherForecast>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        }
        public Task<WeatherForecast> GetCurrentWeatherAsync(double? latitude, double? longitude, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
        public async Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity, CancellationToken ct = default)
        {
            const string sql = @"
                            EXEC dbo.sp_WeatherForecast_Upsert
                                @DateWeather,
                                @Latitude,
                                @Longitude,
                                @TemperatureC,
                                @Summary,
                                @RainfallMm,
                                @Humidity,
                                @WindSpeedKmh,
                                @WeatherMain,
                                @Description,
                                @Icon,
                                @IconUrl,
                                @WeatherType,
                                @IsSevere;";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@DateWeather", entity.DateWeatherUtc);
            parameters.Add("@Latitude", entity.Latitude);
            parameters.Add("@Longitude", entity.Longitude);
            parameters.Add("@TemperatureC", entity.TemperatureC);
            parameters.Add("@Summary", entity.Summary);
            parameters.Add("@RainfallMm", entity.RainfallMm);
            parameters.Add("@Humidity", entity.Humidity);
            parameters.Add("@WindSpeedKmh", entity.WindSpeedKmh);
            parameters.Add("@WeatherMain", entity.WeatherMain);
            parameters.Add("@Description", entity.Description);
            parameters.Add("@Icon", entity.Icon);
            parameters.Add("@IconUrl", entity.IconUrl);
            parameters.Add("@WeatherType", (int)entity.WeatherType);
            parameters.Add("@IsSevere", entity.IsSevere);


            var dateUtc = entity.DateWeatherUtc.Kind == DateTimeKind.Utc
                ? entity.DateWeatherUtc
                : DateTime.SpecifyKind(entity.DateWeatherUtc, DateTimeKind.Utc);

            dateUtc = new DateTime(dateUtc.Ticks - (dateUtc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

            // ✅ Here: we read the SQL return "as a database".
            var row = await _connection.QuerySingleAsync<WeatherForecastReadRow>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));

            // ✅ Here: we rebuild your Domain Entity "as a Domain"
            return new WeatherForecast
            {
                Id = row.Id,
                DateWeatherUtc = DateTime.SpecifyKind(row.DateWeather, DateTimeKind.Utc),
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                TemperatureC = row.TemperatureC,
                Humidity = row.Humidity ?? 0,
                WindSpeedKmh = row.WindSpeedKmh ?? 0,
                RainfallMm = row.RainfallMm ?? 0,
                Summary = row.Summary,
                WeatherMain = row.WeatherMain,
                Description = row.Description,
                Icon = row.Icon,
                IconUrl = row.IconUrl,
                WeatherType = (Contracts.Enums.WeatherType)row.WeatherType,
                IsSevere = row.IsSevere
                // The OpenWeather fields (Icon/WeatherMain/WeatherType…) cannot be filled in.
                // as long as your table/SP doesn't handle them.
            };
        }




        public async Task<WeatherForecast> GenerateNewForecastAsync(CancellationToken ct = default)
        {
            decimal lat = 50.2m + (decimal)_rng.NextDouble() * 0.7m;
            decimal lon = 4.0m + (decimal)_rng.NextDouble() * 1.1m;

            var wf = new WeatherForecast
            {
                DateWeatherUtc = DateTime.UtcNow,
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

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@Now", asOfUtc);
            parameters.Add("@Lat", latitude);
            parameters.Add("@Lon", longitude);
            parameters.Add("@Delta", 0.05m); // WHY: “proximity” ~ 5km (approx)

            var result = await _connection.QuerySingleAsync<(double LastHour, double Last72h)>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));

            return result;
        }
    }
}














































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.