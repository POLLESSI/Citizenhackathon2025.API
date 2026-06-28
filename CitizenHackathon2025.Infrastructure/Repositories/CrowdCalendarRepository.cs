using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;
using System.Text;

namespace CitizenHackathon2025.Infrastructure.Repositories
{
    public sealed class CrowdCalendarRepository : ICrowdCalendarRepository
    {
        private readonly IDbConnection _db;

        public CrowdCalendarRepository(IDbConnection db)
        {
            _db = db;
        }

        public Task<IEnumerable<CrowdCalendarEntry>> GetByDateAsync(DateTime dateUtc, string regionCode, int? placeId = null)
        {
            const string sql = """
                SELECT *
                FROM dbo.CrowdCalendar
                WHERE Active = 1
                  AND DateUtc = @DateUtc
                  AND RegionCode = @RegionCode
                  AND (@PlaceId IS NULL OR PlaceId = @PlaceId);
                """;

            return _db.QueryAsync<CrowdCalendarEntry>(sql, new
            {
                DateUtc = dateUtc.Date,
                RegionCode = regionCode,
                PlaceId = placeId
            });
        }

        public Task<IEnumerable<CrowdCalendarEntry>> GetDueAdvisoriesAsync(DateTime nowUtc, string? regionFilter = null)
        {
            const string sql = """
                DECLARE @TodayUtc DATE = CAST(@NowUtc AS DATE);

                SELECT *
                FROM dbo.CrowdCalendar
                WHERE Active = 1
                  AND DateUtc = @TodayUtc
                  AND (@RegionFilter IS NULL OR RegionCode = @RegionFilter);
                """;

            return _db.QueryAsync<CrowdCalendarEntry>(sql, new
            {
                NowUtc = nowUtc,
                RegionFilter = regionFilter
            });
        }

        public Task<IEnumerable<CrowdCalendarEntry>> GetDueTodayAsync(DateTime nowUtc, string? regionFilter = null)
        {
            const string sql = """
                SELECT *
                FROM dbo.CrowdCalendar
                WHERE Active = 1
                  AND DateUtc = CAST(@NowUtc AS DATE)
                  AND (@RegionFilter IS NULL OR RegionCode = @RegionFilter);
                """;

            return _db.QueryAsync<CrowdCalendarEntry>(sql, new
            {
                NowUtc = nowUtc,
                RegionFilter = regionFilter
            });
        }

        public Task<CrowdCalendarEntry?> GetByIdAsync(int id)
        {
            const string sql = """
                SELECT *
                FROM dbo.CrowdCalendar
                WHERE Id = @Id;
                """;

            return _db.QueryFirstOrDefaultAsync<CrowdCalendarEntry>(sql, new { Id = id });
        }

        public Task<IEnumerable<CrowdCalendarEntry>> ListAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            string? region = null,
            int? placeId = null,
            bool? active = true)
        {
            var sql = new StringBuilder("""
                SELECT *
                FROM dbo.CrowdCalendar
                WHERE 1 = 1
                """);

            var parameters = new DynamicParameters();

            if (fromUtc.HasValue)
            {
                sql.AppendLine("AND DateUtc >= @FromUtc");
                parameters.Add("FromUtc", fromUtc.Value.Date);
            }

            if (toUtc.HasValue)
            {
                sql.AppendLine("AND DateUtc <= @ToUtc");
                parameters.Add("ToUtc", toUtc.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                sql.AppendLine("AND RegionCode = @Region");
                parameters.Add("Region", region);
            }

            if (placeId.HasValue)
            {
                sql.AppendLine("AND PlaceId = @PlaceId");
                parameters.Add("PlaceId", placeId.Value);
            }

            if (active.HasValue)
            {
                sql.AppendLine("AND Active = @Active");
                parameters.Add("Active", active.Value);
            }

            sql.AppendLine("ORDER BY DateUtc, RegionCode, ISNULL(PlaceId, -1);");

            return _db.QueryAsync<CrowdCalendarEntry>(sql.ToString(), parameters);
        }

        public Task<int> InsertAsync(CrowdCalendarEntry entry)
        {
            const string sql = """
                INSERT INTO dbo.CrowdCalendar
                (
                    DateUtc,
                    RegionCode,
                    PlaceId,
                    EventName,
                    ExpectedLevel,
                    Confidence,
                    Latitude,
                    Longitude,
                    StartLocalTime,
                    EndLocalTime,
                    LeadHours,
                    MessageTemplate,
                    Tags,
                    Active
                )
                VALUES
                (
                    @DateUtc,
                    @RegionCode,
                    @PlaceId,
                    @EventName,
                    @ExpectedLevel,
                    @Confidence,
                    @Latitude,
                    @Longitude,
                    @StartLocalTime,
                    @EndLocalTime,
                    @LeadHours,
                    @MessageTemplate,
                    @Tags,
                    @Active
                );
                """;

            return _db.ExecuteAsync(sql, new
            {
                entry.DateUtc,
                entry.RegionCode,
                entry.PlaceId,
                entry.EventName,
                ExpectedLevel = (byte)entry.ExpectedLevel,
                entry.Confidence,
                entry.Latitude,
                entry.Longitude,
                entry.StartLocalTime,
                entry.EndLocalTime,
                entry.LeadHours,
                entry.MessageTemplate,
                entry.Tags,
                entry.Active
            });
        }

        public Task<int> UpdateAsync(CrowdCalendarEntry entry)
        {
            const string sql = """
                UPDATE dbo.CrowdCalendar
                SET
                    DateUtc = @DateUtc,
                    RegionCode = @RegionCode,
                    PlaceId = @PlaceId,
                    EventName = @EventName,
                    ExpectedLevel = @ExpectedLevel,
                    Confidence = @Confidence,
                    Latitude = @Latitude,
                    Longitude = @Longitude,
                    StartLocalTime = @StartLocalTime,
                    EndLocalTime = @EndLocalTime,
                    LeadHours = @LeadHours,
                    MessageTemplate = @MessageTemplate,
                    Tags = @Tags,
                    Active = @Active
                WHERE Id = @Id;
                """;

            return _db.ExecuteAsync(sql, new
            {
                entry.Id,
                entry.DateUtc,
                entry.RegionCode,
                entry.PlaceId,
                entry.EventName,
                ExpectedLevel = (byte)entry.ExpectedLevel,
                entry.Confidence,
                entry.Latitude,
                entry.Longitude,
                entry.StartLocalTime,
                entry.EndLocalTime,
                entry.LeadHours,
                entry.MessageTemplate,
                entry.Tags,
                entry.Active
            });
        }

        public Task<int> UpsertAsync(CrowdCalendarEntry entry)
        {
            const string sql = """
                EXEC dbo.CrowdCalendar_Upsert
                    @DateUtc = @DateUtc,
                    @RegionCode = @RegionCode,
                    @PlaceId = @PlaceId,
                    @EventName = @EventName,
                    @ExpectedLevel = @ExpectedLevel,
                    @Confidence = @Confidence,
                    @Latitude = @Latitude,
                    @Longitude = @Longitude,
                    @StartLocalTime = @StartLocalTime,
                    @EndLocalTime = @EndLocalTime,
                    @LeadHours = @LeadHours,
                    @MessageTemplate = @MessageTemplate,
                    @Tags = @Tags,
                    @Active = @Active;
                """;

            return _db.ExecuteAsync(sql, new
            {
                entry.DateUtc,
                entry.RegionCode,
                entry.PlaceId,
                entry.EventName,
                ExpectedLevel = (byte)entry.ExpectedLevel,
                entry.Confidence,
                entry.Latitude,
                entry.Longitude,
                entry.StartLocalTime,
                entry.EndLocalTime,
                entry.LeadHours,
                entry.MessageTemplate,
                entry.Tags,
                entry.Active
            });
        }

        public Task<int> SoftDeleteAsync(int id)
        {
            const string sql = """
                UPDATE dbo.CrowdCalendar
                SET Active = 0
                WHERE Id = @Id;
                """;

            return _db.ExecuteAsync(sql, new { Id = id });
        }

        public Task<int> RestoreAsync(int id)
        {
            const string sql = """
                UPDATE dbo.CrowdCalendar
                SET Active = 1
                WHERE Id = @Id;
                """;

            return _db.ExecuteAsync(sql, new { Id = id });
        }

        public Task<int> HardDeleteAsync(int id)
        {
            const string sql = """
                DELETE FROM dbo.CrowdCalendar
                WHERE Id = @Id;
                """;

            return _db.ExecuteAsync(sql, new { Id = id });
        }

        public Task<int> ExpireOldEntriesAsync()
        {
            const string sql = "EXEC dbo.CrowdCalendar_ExpireOldEntries;";
            return _db.ExecuteAsync(sql);
        }

        public async Task<IEnumerable<CrowdCalendarEntry>> GetByEventNameAsync(string eventName, bool active = true)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return Enumerable.Empty<CrowdCalendarEntry>();

            const string sql = """
                            SELECT
                                Id,
                                DateUtc,
                                RegionCode,
                                PlaceId,
                                EventName,
                                ExpectedLevel,
                                Confidence,
                                Latitude,
                                Longitude,
                                StartLocalTime,
                                EndLocalTime,
                                LeadHours,
                                MessageTemplate,
                                Tags,
                                Active,
                                CreatedAt
                            FROM dbo.CrowdCalendar
                            WHERE Active = @Active
                              AND EventName IS NOT NULL
                              AND LTRIM(RTRIM(EventName)) <> ''
                              AND EventName LIKE @EventName
                            ORDER BY DateUtc DESC,
                                     RegionCode,
                                     ISNULL(PlaceId,-1);
                            """;

            return await _db.QueryAsync<CrowdCalendarEntry>(
                sql,
                new
                {
                    EventName = BuildLikeParameter(eventName),
                    Active = active
                });
        }

        public Task<IEnumerable<CrowdCalendarEntry>> GetByPlaceIdAsync(int placeId, bool active = true)
        {
            const string sql = """
                            SELECT *
                            FROM dbo.CrowdCalendar
                            WHERE Active = @Active
                                AND PlaceId = @PlaceId
                            ORDER BY DateUtc DESC, RegionCode, ISNULL(PlaceId, -1);
                            """;

            return _db.QueryAsync<CrowdCalendarEntry>(sql, new
            {
                PlaceId = placeId,
                Active = active
            });
        }

        public Task<IEnumerable<CrowdCalendarEntry>> GetByRegionCodeAsync(string regionCode, bool active = true)
        {
            const string sql = """
                            SELECT *
                            FROM dbo.CrowdCalendar
                            WHERE Active = @Active
                                AND RegionCode IS NOT NULL
                                AND LTRIM(RTRIM(RegionCode)) <> ''
                                AND RegionCode LIKE @RegionCode
                            ORDER BY DateUtc DESC, RegionCode, ISNULL(PlaceId, -1);
                            """;

            return _db.QueryAsync<CrowdCalendarEntry>(
                sql,
                new
                {
                    RegionCode = BuildLikeParameter(regionCode),
                    Active = active
                });
        }

        private static string BuildLikeParameter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "%";

            return $"%{value.Trim()}%";
        }
    }
}


























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.