using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citizenhackathon2025.Domain.Entities;
using Citizenhackathon2025.Domain.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Citizenhackathon2025.Infrastructure.Repositories
{
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
    #nullable disable
        private readonly System.Data.IDbConnection _connection;
        private readonly Random _rng = new();

        public WeatherForecastRepository(System.Data.IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task AddAsync(WeatherForecast weatherForecast)
        {
            string sql = "INSERT INTO [WeatherForecast](DateWeather, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh)";
            var parameters = new DynamicParameters();
            //...
            await _connection.ExecuteAsync(sql, parameters);
        }
        public async Task<WeatherForecast?> GetLatestWeatherForecastAsync()
        {
            try
            {
                string sql = "SELECT * FROM WeatherForecast WHERE Active = 1";

                var weatherForecasts = await _connection.QueryAsync<WeatherForecast>(sql);

                return weatherForecasts
                    .OrderByDescending(w => w.DateWeather)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Weather Forecast: {ex.Message}");
                return null;
            }
        }
        public async Task<WeatherForecast> SaveWeatherForecastAsync(WeatherForecast forecast)
        {
            try
            {
                const string sql = @"
                        INSERT INTO WeatherForecast (DateWeather, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh)
                        VALUES (@DateWeather, @TemperatureC, @TemperatureF, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh);
                        SELECT CAST(SCOPE_IDENTITY() AS int);";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@DateWeather", forecast.DateWeather);
                parameters.Add("@TemperatureC", forecast.TemperatureC);
                parameters.Add("@TemperatureF", forecast.TemperatureF);
                parameters.Add("@Summary", forecast.Summary);
                parameters.Add("@RainfallMm", forecast.RainfallMm);
                parameters.Add("@Humidity", forecast.Humidity);
                parameters.Add("@WindSpeedKmh", forecast.WindSpeedKmh);

                // Récupération l'ID généré automatiquement
                var id = await _connection.ExecuteScalarAsync<int>(sql, parameters);
                forecast.Id = id;
                return forecast;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding Weather Forecast: {ex.ToString()}");
                return null;
            }
        }
        public async Task<WeatherForecast?> GetByIdAsync(int id)
        {
            try
            {
                const string sql = "SELECT Id, DateWeather, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh FROM WeatherForecast WHERE Id = @Id AND Active = 1";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id, DbType.Int64);

                var weatherForecast = await _connection.QueryFirstOrDefaultAsync<WeatherForecast?>(sql, parameters);

                return weatherForecast;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error geting Weather Forecast : {ex.ToString}");
                return null;
            }

        }
        public async Task<List<WeatherForecast>> GetAllAsync()
        {
            try
            {
                string sql = " SELECT TOP 10 Id, DateWeather, TemperatureC, TemperatureF, Summary, RainfallMm, Humidity, WindSpeedKmh FROM WeatherForecast WHERE Active = 1 ORDER BY DateEvent DESC";

                var weatherForecasts = await _connection.QueryAsync<WeatherForecast?>(sql);
                return [.. weatherForecasts];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving WeatherForecasts: {ex.Message}");
                return [];
            }
        }
        public async Task<WeatherForecast> GenerateNewForecastAsync()
        {
            var forecast = new WeatherForecast
            {
                DateWeather = DateTime.Now,
                TemperatureC = _rng.Next(-5, 35),
                Summary = "Genered",
                RainfallMm = Math.Round(_rng.NextDouble() * 20, 1),
                Humidity = _rng.Next(30, 100),
                WindSpeedKmh = Math.Round(_rng.NextDouble() * 80, 1)
            };

            await SaveWeatherForecastAsync(forecast);
            return forecast;
        }
        
        public async Task<List<WeatherForecast>> GetHistoryAsync(int limit = 128)
        {
            try
            {
                const string sql = @"
                        SELECT * 
                        FROM WeatherForecast
                        WHERE Active = 1
                        ORDER BY DateWeather DESC";

                var results = await _connection.QueryAsync<WeatherForecast>(sql, new { Limit = limit });
                return results.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving weather history : {ex.Message}");
                return new List<WeatherForecast>();
            }
        }
    }
}













































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.