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
                                (DateWeather, TemperatureC, TemperatureF, Summary, RainfallNm, Humidity, WindSpeedKmh, Active)
                            VALUES
                                (@DateWeather, @TemperatureC, @TemperatureF, @Summary, @RainfallNm, @Humidity, @WindSpeedKmh, 1);";
            DynamicParameters parameters = new();
            parameters.Add("DateWeather", wf.DateWeather, DbType.DateTime2);
            parameters.Add("TemperatureC", wf.TemperatureC, DbType.Int32);
            parameters.Add("TemperatureF", wf.TemperatureF, DbType.Int32); // locally calculated value (getter)
            parameters.Add("Summary", wf.Summary, DbType.String);
            parameters.Add("RainfallNm", wf.RainfallMm, DbType.Double);    // alias Nm <-> Mm
            parameters.Add("Humidity", wf.Humidity, DbType.Int32);
            parameters.Add("WindSpeedKmh", wf.WindSpeedKmh, DbType.Double);

            await _connection.ExecuteAsync(sql, parameters);
        }

        public async Task<WeatherForecast?> GetLatestWeatherForecastAsync()
        {
            const string sql = @"
                            SELECT TOP(1)
                                Id, DateWeather, TemperatureC, TemperatureF,
                                Summary,
                                RainfallNm AS RainfallMm,      -- alias !
                                Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Active = 1
                            ORDER BY DateWeather DESC;";
            DynamicParameters parameters = new();
            parameters.Add("Limit", 1, DbType.Int32);
            
            return await _connection.QueryFirstOrDefaultAsync<WeatherForecast>(sql, parameters);
        }

        public async Task<WeatherForecast> SaveOrUpdateAsync(WeatherForecast entity)
        {
            // Upsert based on DateWeather (unique)
            var tempF = entity.TemperatureF;

            const string merge = @"
                            MERGE WeatherForecast AS t
                            USING (SELECT @DateWeather AS DateWeather) AS s
                            ON (t.DateWeather = s.DateWeather)
                            WHEN MATCHED THEN
                                UPDATE SET
                                    TemperatureC = @TemperatureC,
                                    TemperatureF = @TemperatureF,
                                    Summary      = @Summary,
                                    RainfallNm   = @RainfallNm,
                                    Humidity     = @Humidity,
                                    WindSpeedKmh = @WindSpeedKmh
                            WHEN NOT MATCHED THEN
                                INSERT (DateWeather, TemperatureC, TemperatureF, Summary, RainfallNm, Humidity, WindSpeedKmh, Active)
                                VALUES (@DateWeather, @TemperatureC, @TemperatureF, @Summary, @RainfallNm, @Humidity, @WindSpeedKmh, 1)
                            OUTPUT inserted.Id;";
            DynamicParameters parameters = new();
            parameters.Add("DateWeather", entity.DateWeather, DbType.DateTime2);
            parameters.Add("TemperatureC", entity.TemperatureC, DbType.Int32);
            parameters.Add("TemperatureF", tempF, DbType.Int32); // locally calculated value (getter)
            parameters.Add("Summary", entity.Summary, DbType.String);
            parameters.Add("RainfallNm", entity.RainfallMm, DbType.Double);    // alias Nm <-> Mm
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
                            SELECT Id, DateWeather, TemperatureC, TemperatureF, Summary,
                                   RainfallNm AS RainfallMm, Humidity, WindSpeedKmh, Active
                            FROM WeatherForecast
                            WHERE Id = @Id AND Active = 1;";
            DynamicParameters parameters = new();
            parameters.Add("Id", id, DbType.Int32);

            return await _connection.QueryFirstOrDefaultAsync<WeatherForecast>(sql, parameters);
        }

        public async Task<List<WeatherForecast>> GetAllAsync()
        {
            const string sql = @"
                            SELECT Id, DateWeather, TemperatureC, TemperatureF, Summary,
                                   RainfallNm AS RainfallMm, Humidity, WindSpeedKmh, Active
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
                                Id, DateWeather, TemperatureC, TemperatureF, Summary,
                                RainfallNm AS RainfallMm, Humidity, WindSpeedKmh, Active
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