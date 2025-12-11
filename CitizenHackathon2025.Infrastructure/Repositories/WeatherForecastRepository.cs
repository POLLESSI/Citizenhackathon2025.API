using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<EventRepository> _logger;
        private readonly Random _rng = new();

        public WeatherForecastRepository(IDbConnection connection, ILogger<EventRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task AddAsync(WeatherForecast wf)
        {
            const string sql = @"
        INSERT INTO WeatherForecast
            (DateWeather, Latitude, Longitude, TemperatureC, Summary, RainfallMm, Humidity, WindSpeedKmh, Active)
        VALUES
            (@DateWeather, @Latitude, @Longitude, @TemperatureC, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh, 1);";

            DynamicParameters parameters = new();
            parameters.Add("DateWeather", wf.DateWeather, DbType.DateTime2);
            parameters.Add("Latitude", wf.Latitude, DbType.Decimal);
            parameters.Add("Longitude", wf.Longitude, DbType.Decimal);
            parameters.Add("TemperatureC", wf.TemperatureC, DbType.Int32);
            parameters.Add("Summary", wf.Summary, DbType.String);
            parameters.Add("RainfallMm", wf.RainfallMm, DbType.Double);
            parameters.Add("Humidity", wf.Humidity, DbType.Int32);
            parameters.Add("WindSpeedKmh", wf.WindSpeedKmh, DbType.Double);

            await _connection.ExecuteAsync(sql, parameters);
        }


        public Task<WeatherForecast?> GetLatestWeatherForecastAsync(CancellationToken ct = default)
        {
            const string sql = @"
        SELECT TOP(1)
            Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
        FROM dbo.WeatherForecast
        WHERE Active = 1
        ORDER BY DateWeather DESC;";
            return _connection.QueryFirstOrDefaultAsync<WeatherForecast>(new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity)
        {
            const string sql = @"EXEC dbo.sp_WeatherForecast_Upsert
                         @DateWeather, @Latitude, @Longitude, @TemperatureC, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh;";

            var parameters = new DynamicParameters();
            parameters.Add("@DateWeather", entity.DateWeather, DbType.DateTime2);
            parameters.Add("@Latitude", entity.Latitude, DbType.Decimal);
            parameters.Add("@Longitude", entity.Longitude, DbType.Decimal);
            parameters.Add("@TemperatureC", entity.TemperatureC, DbType.Int32);
            parameters.Add("@Summary", entity.Summary, DbType.String);
            parameters.Add("@RainfallMm", entity.RainfallMm, DbType.Double);
            parameters.Add("@Humidity", entity.Humidity, DbType.Int32);
            parameters.Add("@WindSpeedKmh", entity.WindSpeedKmh, DbType.Double);

            var saved = await _connection.QuerySingleAsync<WeatherForecast>(sql, parameters);
            return saved;
        }


        public async Task<WeatherForecast> SaveWeatherForecastAsync(WeatherForecast forecast)
            => await SaveOrUpdateAsync(forecast);

        public async Task<WeatherForecast?> GetByIdAsync(int id)
        {
            const string sql = @"
                            SELECT Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Id = @Id AND Active = 1;";
            DynamicParameters parameters = new();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryFirstOrDefaultAsync<WeatherForecast>(sql, parameters);
        }



        public async Task<List<WeatherForecast>> GetAllAsync()
        {
            const string sql = @"
                            SELECT Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";

            // The Limit parameter is unnecessary here; you can remove it or keep it for later.
            var rows = await _connection.QueryAsync<WeatherForecast>(sql);
            return rows.ToList();
        }


        public async Task<WeatherForecast> GenerateNewForecastAsync()
        {
            // Small area around Wallonia/Brussels :
            // lat ~ [50.2, 50.9], lon ~ [4.0, 5.1]
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

            return await SaveOrUpdateAsync(wf);
        }


        public async Task<List<WeatherForecast>> GetHistoryAsync(int limit = 128)
        {
            const string sql = @"
                        SELECT TOP(@Limit) Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh,Active
                        FROM WeatherForecast
                        WHERE Active = 1
                        ORDER BY DateWeather DESC;";

            DynamicParameters parameters = new();
            parameters.Add("Limit", limit, DbType.Int32);

            var rows = await _connection.QueryAsync<WeatherForecast>(sql, parameters);
            return rows.ToList();
        }

        public async Task<(double LastHour, double Last72h)> GetRainAccumulationAsync(decimal lat, decimal lon, DateTime asOfUtc)
        {
            const string sql = @"
                        SELECT
                            SUM(CASE WHEN DateWeather >= @From1h  THEN ISNULL(RainfallMm, 0) ELSE 0 END) AS Rain1h,
                            SUM(CASE WHEN DateWeather >= @From72h THEN ISNULL(RainfallMm, 0) ELSE 0 END) AS Rain72h
                        FROM WeatherForecast
                        WHERE Active = 1
                          AND ABS(Latitude  - @Lat) <= @Delta
                          AND ABS(Longitude - @Lon) <= @Delta;";

            var p = new DynamicParameters();
            p.Add("@From1h", asOfUtc.AddHours(-1), DbType.DateTime2);
            p.Add("@From72h", asOfUtc.AddHours(-72), DbType.DateTime2);
            p.Add("@Lat", lat, DbType.Decimal);
            p.Add("@Lon", lon, DbType.Decimal);
            p.Add("@Delta", 0.05m, DbType.Decimal); // ~5 km on each side (approx.)

            var result = await _connection.QuerySingleAsync<(double Rain1h, double Rain72h)>(sql, p);
            return (result.Rain1h, result.Rain72h);
        }


        public async Task<int> ArchivePastWeatherForecastsAsync()
        {
            const string sql = @"
                        UPDATE [WeatherForecast]
                        SET [Active] = 0
                        WHERE [Active] = 1
                          AND [DateWeather] < DATEADD(DAY, -1, CAST(GETDATE() AS DATETIME2(0)));";

            try
            {
                var affectedRows = await _connection.ExecuteAsync(sql);
                _logger.LogInformation("{Count} Weather Forecast(s) archived.", affectedRows);
                return affectedRows;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error archiving past Weather Forecasts.");
                return 0;
            }
        }

        public async Task<(double LastHour, double Last72h)> GetRainAccumulationAsync( decimal latitude, decimal longitude, DateTime now, CancellationToken ct = default)
        {
            const string sql = @"
                        SELECT 
                            SUM(CASE WHEN DateWeather >= DATEADD(HOUR, -1, @Now) THEN RainfallMm ELSE 0 END) AS LastHour,
                            SUM(CASE WHEN DateWeather >= DATEADD(HOUR, -72, @Now) THEN RainfallMm ELSE 0 END) AS Last72h
                        FROM WeatherForecast
                        WHERE Active = 1
                          AND ISNULL(Latitude, 0) = @Lat
                          AND ISNULL(Longitude, 0) = @Lon;
    ";

            var result = await _connection.QueryFirstAsync<(double LastHour, double Last72h)>(
                new CommandDefinition(sql, new { Now = now, Lat = latitude, Lon = longitude }, cancellationToken: ct));

            return result;
        }

    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.