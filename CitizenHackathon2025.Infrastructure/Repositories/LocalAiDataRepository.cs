using CitizenHackathon2025.Domain.DTOs;
using CitizenHackathon2025.Domain.Interfaces;
using CitizenHackathon2025.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class LocalAiDataRepository : ILocalAiDataRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public LocalAiDataRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private async Task<IDbConnection> OpenConnectionAsync(CancellationToken ct)
        {
            var connection = _connectionFactory.CreateConnection();

            if (connection is SqlConnection sqlConnection)
            {
                await sqlConnection.OpenAsync(ct);
                return sqlConnection;
            }

            connection.Open();
            return connection;
        }

        public async Task<IEnumerable<LocalAiCrowdCalendarContextDTO>> GetNearbyCrowdCalendarAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default)
        {
            const string sql = @"
                            WITH CrowdCalendarBase AS
                            (
                                SELECT
                                    cc.Id,
                                    cc.DateUtc,
                                    cc.RegionCode,
                                    cc.PlaceId,
                                    cc.EventName,
                                    ExpectedLevel = CAST(cc.ExpectedLevel AS int),
                                    Confidence = CAST(cc.Confidence AS int),
                                    cc.StartLocalTime,
                                    cc.EndLocalTime,
                                    cc.LeadHours,
                                    cc.MessageTemplate,
                                    cc.Tags,
                                    Latitude = CAST(cc.Latitude AS float),
                                    Longitude = CAST(cc.Longitude AS float),
                                    cc.Active,
                                    DistanceKm =
                                        6371.0 * ACOS(
                                            CASE
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(cc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(cc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(cc.Latitude AS float))) > 1
                                                THEN 1
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(cc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(cc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(cc.Latitude AS float))) < -1
                                                THEN -1
                                                ELSE
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(cc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(cc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(cc.Latitude AS float)))
                                            END
                                        )
                                FROM dbo.CrowdCalendar cc
                                WHERE cc.Active = 1
                                  AND cc.DateUtc BETWEEN @TargetDate AND DATEADD(DAY, 1, @TargetDate)
                            )
                            SELECT TOP (8)
                                Id,
                                DateUtc,
                                RegionCode,
                                PlaceId,
                                EventName,
                                ExpectedLevel,
                                Confidence,
                                StartLocalTime,
                                EndLocalTime,
                                LeadHours,
                                MessageTemplate,
                                Tags,
                                Latitude,
                                Longitude,
                                DistanceKm,
                                Active
                            FROM CrowdCalendarBase
                            WHERE DistanceKm <= @RadiusKm
                            ORDER BY DistanceKm ASC, ExpectedLevel DESC, Confidence DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Lat", latitude, DbType.Double);
            parameters.Add("@Lng", longitude, DbType.Double);
            parameters.Add("@TargetDate", targetDate.Date, DbType.Date);
            parameters.Add("@RadiusKm", radiusKm, DbType.Double);

            using var connection = await OpenConnectionAsync(ct);
            return await connection.QueryAsync<LocalAiCrowdCalendarContextDTO>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<LocalAiCrowdInfoContextDTO>> GetNearbyCrowdInfoAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default)
        {
            const string sql = @"
                            WITH CrowdInfoBase AS
                            (
                                SELECT
                                    ci.Id,
                                    ci.LocationName,
                                    Latitude = CAST(ci.Latitude AS float),
                                    Longitude = CAST(ci.Longitude AS float),
                                    CrowdLevel = CAST(ci.CrowdLevel AS int),
                                    [Timestamp] = ci.[Timestamp],
                                    ci.Active,
                                    DistanceKm =
                                        6371.0 * ACOS(
                                            CASE
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(ci.Latitude AS float))) *
                                                    COS(RADIANS(CAST(ci.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(ci.Latitude AS float))) > 1
                                                THEN 1
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(ci.Latitude AS float))) *
                                                    COS(RADIANS(CAST(ci.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(ci.Latitude AS float))) < -1
                                                THEN -1
                                                ELSE
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(ci.Latitude AS float))) *
                                                    COS(RADIANS(CAST(ci.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(ci.Latitude AS float)))
                                            END
                                        )
                                FROM dbo.CrowdInfo ci
                                WHERE ci.Active = 1
                                  AND CAST(ci.[Timestamp] AS date) BETWEEN DATEADD(DAY, -1, @TargetDate) AND DATEADD(DAY, 1, @TargetDate)
                            )
                            SELECT TOP (8)
                                Id,
                                LocationName,
                                Latitude,
                                Longitude,
                                CrowdLevel,
                                [Timestamp],
                                DistanceKm,
                                Active
                            FROM CrowdInfoBase
                            WHERE DistanceKm <= @RadiusKm
                            ORDER BY [Timestamp] DESC, DistanceKm ASC, CrowdLevel DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Lat", latitude, DbType.Double);
            parameters.Add("@Lng", longitude, DbType.Double);
            parameters.Add("@TargetDate", targetDate.Date, DbType.Date);
            parameters.Add("@RadiusKm", radiusKm, DbType.Double);

            using var connection = await OpenConnectionAsync(ct);
            return await connection.QueryAsync<LocalAiCrowdInfoContextDTO>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<LocalAiEventContextDTO>> GetNearbyEventsAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default)
        {
            const string sql = @"
                            WITH EventBase AS
                            (
                                SELECT
                                    cc.Id,
                                    City = cc.RegionCode,
                                    Title = cc.EventName,
                                    EventDate = CAST(cc.DateUtc AS datetime2),
                                    StartTime = cc.StartLocalTime,
                                    EndTime = cc.EndLocalTime,
                                    CrowdLevel = CAST(cc.ExpectedLevel AS int),
                                    MaxCapacity = CAST(cc.Confidence AS int),
                                    Advice = cc.MessageTemplate,
                                    Latitude = CAST(cc.Latitude AS float),
                                    Longitude = CAST(cc.Longitude AS float),
                                    DistanceKm =
                                        6371.0 * ACOS(
                                            CASE
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(cc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(cc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(cc.Latitude AS float))) > 1
                                                THEN 1
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(cc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(cc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(cc.Latitude AS float))) < -1
                                                THEN -1
                                                ELSE
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(cc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(cc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(cc.Latitude AS float)))
                                            END
                                        )
                                FROM dbo.CrowdCalendar cc
                                WHERE cc.Active = 1
                                    AND cc.DateUtc BETWEEN @TargetDate AND DATEADD(DAY, 1, @TargetDate)
                                    AND cc.Latitude IS NOT NULL
                                    AND cc.Longitude IS NOT NULL
                            )
                            SELECT TOP (8)
                                Id,
                                City,
                                Title,
                                EventDate,
                                StartTime,
                                EndTime,
                                CrowdLevel,
                                MaxCapacity,
                                Advice,
                                Latitude,
                                Longitude,
                                DistanceKm
                            FROM EventBase
                            WHERE DistanceKm <= @RadiusKm
                            ORDER BY DistanceKm ASC, CrowdLevel DESC, MaxCapacity DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Lat", latitude, DbType.Double);
            parameters.Add("@Lng", longitude, DbType.Double);
            parameters.Add("@TargetDate", targetDate.Date, DbType.Date);
            parameters.Add("@RadiusKm", radiusKm, DbType.Double);

            using var connection = await OpenConnectionAsync(ct);
            return await connection.QueryAsync<LocalAiEventContextDTO>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<LocalAiTrafficContextDTO>> GetNearbyTrafficAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default)
        {
            const string sql = @"
                            WITH TrafficBase AS
                            (
                                SELECT
                                    tc.Id,
                                    tc.DateCondition,
                                    tc.CongestionLevel,
                                    tc.IncidentType,
                                    tc.Provider,
                                    tc.ExternalId,
                                    tc.Title,
                                    tc.Road,
                                    Severity = CAST(tc.Severity AS int),
                                    Latitude = CAST(tc.Latitude AS float),
                                    Longitude = CAST(tc.Longitude AS float),
                                    tc.Active,
                                    DistanceKm =
                                        6371.0 * ACOS(
                                            CASE
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(tc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(tc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(tc.Latitude AS float))) > 1
                                                THEN 1
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(tc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(tc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(tc.Latitude AS float))) < -1
                                                THEN -1
                                                ELSE
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(tc.Latitude AS float))) *
                                                    COS(RADIANS(CAST(tc.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(tc.Latitude AS float)))
                                            END
                                        )
                                FROM dbo.TrafficCondition tc
                                WHERE tc.Active = 1
                                  AND CAST(tc.DateCondition AS date) BETWEEN DATEADD(DAY, -1, @TargetDate) AND DATEADD(DAY, 1, @TargetDate)
                            )
                            SELECT TOP (5)
                                Id,
                                DateCondition,
                                CongestionLevel,
                                IncidentType,
                                Provider,
                                ExternalId,
                                Title,
                                Road,
                                Severity,
                                Latitude,
                                Longitude,
                                DistanceKm,
                                Active
                            FROM TrafficBase
                            WHERE DistanceKm <= @RadiusKm
                            ORDER BY Severity DESC, DistanceKm ASC, DateCondition DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Lat", latitude, DbType.Double);
            parameters.Add("@Lng", longitude, DbType.Double);
            parameters.Add("@TargetDate", targetDate.Date, DbType.Date);
            parameters.Add("@RadiusKm", radiusKm, DbType.Double);

            using var connection = await OpenConnectionAsync(ct);
            return await connection.QueryAsync<LocalAiTrafficContextDTO>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }

        public async Task<IEnumerable<LocalAiWeatherContextDTO>> GetNearbyWeatherAsync(
            double latitude,
            double longitude,
            DateTime targetDate,
            double radiusKm,
            CancellationToken ct = default)
        {
            const string sql = @"
                            WITH WeatherBase AS
                            (
                                SELECT
                                    wf.Id,
                                    DateWeather = wf.DateWeather,
                                    Latitude = CAST(wf.Latitude AS float),
                                    Longitude = CAST(wf.Longitude AS float),
                                    TemperatureC = CAST(wf.TemperatureC AS int),
                                    TemperatureF = CAST(wf.TemperatureF AS int),
                                    wf.Summary,
                                    RainfallMm = CAST(wf.RainfallMm AS float),
                                    Humidity = CAST(wf.Humidity AS int),
                                    WindSpeedKmh = CAST(wf.WindSpeedKmh AS float),
                                    wf.WeatherMain,
                                    wf.Description,
                                    wf.Icon,
                                    wf.IconUrl,
                                    WeatherType = CAST(wf.WeatherType AS int),
                                    IsSevere = CAST(wf.IsSevere AS bit),
                                    wf.Active,
                                    DistanceKm =
                                        6371.0 * ACOS(
                                            CASE
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(wf.Latitude AS float))) *
                                                    COS(RADIANS(CAST(wf.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(wf.Latitude AS float))) > 1
                                                THEN 1
                                                WHEN
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(wf.Latitude AS float))) *
                                                    COS(RADIANS(CAST(wf.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(wf.Latitude AS float))) < -1
                                                THEN -1
                                                ELSE
                                                    COS(RADIANS(@Lat)) * COS(RADIANS(CAST(wf.Latitude AS float))) *
                                                    COS(RADIANS(CAST(wf.Longitude AS float)) - RADIANS(@Lng)) +
                                                    SIN(RADIANS(@Lat)) * SIN(RADIANS(CAST(wf.Latitude AS float)))
                                            END
                                        )
                                FROM dbo.WeatherForecast wf
                                WHERE wf.Active = 1
                                  AND CAST(wf.DateWeather AS date) BETWEEN @TargetDate AND DATEADD(DAY, 1, @TargetDate)
                            )
                            SELECT TOP (3)
                                Id,
                                DateWeather,
                                Latitude,
                                Longitude,
                                TemperatureC,
                                TemperatureF,
                                Summary,
                                RainfallMm,
                                Humidity,
                                WindSpeedKmh,
                                WeatherMain,
                                Description,
                                Icon,
                                IconUrl,
                                WeatherType,
                                IsSevere,
                                DistanceKm,
                                Active
                            FROM WeatherBase
                            WHERE DistanceKm <= @RadiusKm
                            ORDER BY DistanceKm ASC, DateWeather ASC;";

            var parameters = new DynamicParameters();
            parameters.Add("@Lat", latitude, DbType.Double);
            parameters.Add("@Lng", longitude, DbType.Double);
            parameters.Add("@TargetDate", targetDate.Date, DbType.Date);
            parameters.Add("@RadiusKm", radiusKm, DbType.Double);

            using var connection = await OpenConnectionAsync(ct);
            return await connection.QueryAsync<LocalAiWeatherContextDTO>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }
    }
}



































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.