using System.Data;
using Dapper;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
        private readonly IDbConnection _connection;
        private readonly Random _rng = new();

        public WeatherForecastRepository(IDbConnection connection) => _connection = connection;

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
            Id, DateWeather, Latitude, Longitude, TemperatureC, Summary, RainfallMm, Humidity, WindSpeedKmh, Active
        FROM dbo.WeatherForecast
        WHERE Active = 1
        ORDER BY DateWeather DESC;";
            return _connection.QueryFirstOrDefaultAsync<WeatherForecast>(new CommandDefinition(sql, cancellationToken: ct));
        }

        public async Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity)
        {
            const string merge = @"
                            MERGE WeatherForecast AS t
                            USING (SELECT @DateWeather AS DateWeather) AS s
                            ON (t.DateWeather = s.DateWeather)
                            WHEN MATCHED THEN
                                UPDATE SET
                                    Latitude     = @Latitude,
                                    Longitude    = @Longitude,
                                    TemperatureC = @TemperatureC,
                                    Summary      = @Summary,
                                    RainfallMm   = @RainfallMm,
                                    Humidity     = @Humidity,
                                    WindSpeedKmh = @WindSpeedKmh
                            WHEN NOT MATCHED THEN
                                INSERT (DateWeather, Latitude, Longitude, TemperatureC, Summary, RainfallMm, Humidity, WindSpeedKmh, Active)
                                VALUES (@DateWeather, @Latitude, @Longitude, @TemperatureC, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh, 1)
                            OUTPUT inserted.Id;";

            DynamicParameters parameters = new();
            parameters.Add("DateWeather", entity.DateWeather, DbType.DateTime2);
            parameters.Add("Latitude", entity.Latitude, DbType.Decimal);
            parameters.Add("Longitude", entity.Longitude, DbType.Decimal);
            parameters.Add("TemperatureC", entity.TemperatureC, DbType.Int32);
            parameters.Add("Summary", entity.Summary, DbType.String);
            parameters.Add("RainfallMm", entity.RainfallMm, DbType.Double);
            parameters.Add("Humidity", entity.Humidity, DbType.Int32);
            parameters.Add("WindSpeedKmh", entity.WindSpeedKmh, DbType.Double);

            entity.Id = await _connection.ExecuteScalarAsync<int>(merge, parameters);
            return entity;
        }

        public async Task<WeatherForecast> SaveWeatherForecastAsync(WeatherForecast forecast)
            => await SaveOrUpdateAsync(forecast);

        public async Task<WeatherForecast?> GetByIdAsync(int id)
        {
            const string sql = @"
                            SELECT Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary,
                                   RainfallMm AS RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Id = @Id AND Active = 1;";
            DynamicParameters parameters = new();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryFirstOrDefaultAsync<WeatherForecast>(sql, parameters);
        }

        public async Task<List<WeatherForecast>> GetAllAsync()
        {
            const string sql = @"
                            SELECT Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary,
                                   RainfallMm AS RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";
            DynamicParameters parameters = new();
            parameters.Add("Limit", 256, DbType.Int32);

            var rows = await _connection.QueryAsync<WeatherForecast>(sql, parameters);
            return rows.ToList();
        }

        public async Task<WeatherForecast> GenerateNewForecastAsync()
        {
            var wf = new WeatherForecast
            {
                DateWeather = DateTime.UtcNow,
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
                            SELECT TOP(@Limit)
                                Id, DateWeather, Latitude, Longitude, TemperatureC, TemperatureF, Summary,
                                RainfallMm AS RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";
            DynamicParameters parameters = new();
            parameters.Add("Limit", limit, DbType.Int32);

            var rows = await _connection.QueryAsync<WeatherForecast>(sql, parameters);
            return rows.ToList();
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.