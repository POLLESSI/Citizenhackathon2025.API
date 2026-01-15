using CitizenHackathon2025.Domain.Abstractions;
using CitizenHackathon2025.Domain.Entities;
using CitizenHackathon2025.Domain.Interfaces;
using Dapper;
using System.Data;
using System.Text;

public class CrowdCalendarRepository : ICrowdCalendarRepository
{
    private readonly IDbConnection _db;
    private readonly ITimeZoneConverter _tz; // optional if you manage TZ

    public CrowdCalendarRepository(IDbConnection db /*, ITimeZoneConverter tz */) { _db = db; /* _tz = tz; */ }

    public Task<IEnumerable<CrowdCalendarEntry>> GetByDateAsync(DateTime dateUtc, string regionCode, int? placeId = null)
    {
        const string sql = @"
                        SELECT *
                        FROM dbo.CrowdCalendar
                        WHERE Active = 1
                          AND DateUtc = @d
                          AND RegionCode = @r
                          AND (@p IS NULL OR PlaceId = @p)";
        return _db.QueryAsync<CrowdCalendarEntry>(sql, new { d = dateUtc.Date, r = regionCode, p = placeId });
    }

    public Task<CrowdCalendarEntry?> GetByIdAsync(int id) =>
        _db.QueryFirstOrDefaultAsync<CrowdCalendarEntry>(@"SELECT * FROM dbo.CrowdCalendar WHERE Id=@id", new { id });

    // All entries with the "starts now" alert window
    public Task<IEnumerable<CrowdCalendarEntry>> GetDueAdvisoriesAsync(DateTime nowUtc, string? regionFilter = null)
    {
        // Hypothesis: Europe/Brussels time zone for StartLocalTime
        const string sql = @"
                        DECLARE @todayUtc DATE = CAST(@nowUtc AS DATE);

                        SELECT *
                        FROM dbo.CrowdCalendar
                        WHERE Active = 1
                          AND DateUtc = @todayUtc
                          AND (@region IS NULL OR RegionCode = @region)";
        // We filter on the service side for the time slot (LeadHours vs StartLocalTime) if we prefer
        return _db.QueryAsync<CrowdCalendarEntry>(sql, new { nowUtc, region = regionFilter });
    }

    public Task<int> HardDeleteAsync(int id) =>
        _db.ExecuteAsync(@"DELETE FROM dbo.CrowdCalendar WHERE Id=@id", new { id });

    public async Task<IEnumerable<CrowdCalendarEntry>> GetDueTodayAsync(DateTime nowUtc, string? regionFilter = null)
    {
        // We retrieve the active entries of the day (fine filter done on the alert service side)
        var sql = @"SELECT * FROM dbo.CrowdCalendar
                    WHERE Active = 1 AND DateUtc = CAST(@nowUtc AS date)
                      AND (@region IS NULL OR RegionCode = @region)";
        return await _db.QueryAsync<CrowdCalendarEntry>(sql, new { nowUtc, region = regionFilter });
    }

    public Task<int> InsertAsync(CrowdCalendarEntry e) =>
        _db.ExecuteAsync(
            @"INSERT INTO dbo.CrowdCalendar
              (DateUtc, RegionCode, PlaceId, EventName, ExpectedLevel, Confidence, Latitude, Longitude, StartLocalTime, EndLocalTime, LeadHours, MessageTemplate, Tags, Active)
              VALUES (@DateUtc, @RegionCode, @PlaceId, @EventName, @ExpectedLevel, @Confidence, @Latitude, @Longitude, @StartLocalTime, @EndLocalTime, @LeadHours, @MessageTemplate, @Tags, @Active);",
            new
            {
                e.DateUtc,
                e.RegionCode,
                e.PlaceId,
                e.EventName,
                ExpectedLevel = (byte)e.ExpectedLevel,
                e.Confidence,
                e.Latitude,
                e.Longitude,
                e.StartLocalTime,
                e.EndLocalTime,
                e.LeadHours,
                e.MessageTemplate,
                e.Tags,
                e.Active
            });

    public Task<IEnumerable<CrowdCalendarEntry>> ListAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string? region = null, int? placeId = null, bool? active = true)
    {
        var sql = new StringBuilder("SELECT * FROM dbo.CrowdCalendar WHERE 1=1 ");
        var p = new DynamicParameters();

        if (fromUtc is not null) { sql.Append(" AND DateUtc >= @from "); p.Add("from", fromUtc.Value.Date); }
        if (toUtc is not null) { sql.Append(" AND DateUtc <= @to "); p.Add("to", toUtc.Value.Date); }
        if (!string.IsNullOrWhiteSpace(region)) { sql.Append(" AND RegionCode = @r "); p.Add("r", region); }
        if (placeId is not null) { sql.Append(" AND PlaceId = @pid "); p.Add("pid", placeId); }
        if (active is not null) { sql.Append(" AND Active = @a "); p.Add("a", active.Value); }

        sql.Append(" ORDER BY DateUtc, RegionCode, ISNULL(PlaceId, -1)");
        return _db.QueryAsync<CrowdCalendarEntry>(sql.ToString(), p);
    }

    public Task<int> RestoreAsync(int id) =>
        _db.ExecuteAsync(@"UPDATE dbo.CrowdCalendar SET Active = 1 WHERE Id=@id", new { id });

    public Task<int> SoftDeleteAsync(int id) =>
        _db.ExecuteAsync(@"UPDATE dbo.CrowdCalendar SET Active = 0 WHERE Id=@id", new { id });

    public Task<int> UpdateAsync(CrowdCalendarEntry e) =>
        _db.ExecuteAsync(
            @"UPDATE dbo.CrowdCalendar SET
                DateUtc=@DateUtc, RegionCode=@RegionCode, PlaceId=@PlaceId,
                EventName=@EventName, ExpectedLevel=@ExpectedLevel, Confidence=@Confidence,
                StartLocalTime=@StartLocalTime, EndLocalTime=@EndLocalTime, LeadHours=@LeadHours,
                MessageTemplate=@MessageTemplate, Tags=@Tags, Active=@Active
              WHERE Id=@Id;",
            new
            {
                e.DateUtc,
                e.RegionCode,
                e.PlaceId,
                e.EventName,
                ExpectedLevel = (byte)e.ExpectedLevel,
                e.Confidence,
                e.StartLocalTime,
                e.EndLocalTime,
                e.LeadHours,
                e.MessageTemplate,
                e.Tags,
                e.Active,
                e.Id
            });

    public Task<int> UpsertAsync(CrowdCalendarEntry e) =>
        _db.ExecuteAsync(
            @"EXEC dbo.CrowdCalendar_Upsert
                @DateUtc=@DateUtc, @RegionCode=@RegionCode, @PlaceId=@PlaceId, @EventName=@EventName,
                @ExpectedLevel=@ExpectedLevel, @Confidence=@Confidence, @Latitude=@Latitude, @Longitude=@Longitude, @StartLocalTime=@StartLocalTime,
                @EndLocalTime=@EndLocalTime, @LeadHours=@LeadHours, @MessageTemplate=@MessageTemplate,
                @Tags=@Tags, @Active=@Active;",
            new
            {
                e.DateUtc,
                e.RegionCode,
                e.PlaceId,
                e.EventName,
                ExpectedLevel = (byte)e.ExpectedLevel,
                e.Confidence,
                e.Latitude,
                e.Longitude,
                e.StartLocalTime,
                e.EndLocalTime,
                e.LeadHours,
                e.MessageTemplate,
                e.Tags,
                e.Active
            });
}


























































































































// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.