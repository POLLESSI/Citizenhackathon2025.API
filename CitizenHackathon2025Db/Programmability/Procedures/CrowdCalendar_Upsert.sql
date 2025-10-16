-- Upsert procedure
CREATE PROCEDURE dbo.CrowdCalendar_Upsert
    @DateUtc          date,
    @RegionCode       nvarchar(32),
    @PlaceId          int            = NULL,
    @EventName        nvarchar(128)  = NULL,
    @ExpectedLevel    tinyint,
    @Confidence       tinyint        = NULL,
    @StartLocalTime   time(0)        = NULL,
    @EndLocalTime     time(0)        = NULL,
    @LeadHours        int            = 3,
    @MessageTemplate  nvarchar(512)  = NULL,
    @Tags             nvarchar(128)  = NULL,
    @Active           bit            = 1
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.CrowdCalendar AS tgt
    USING (SELECT
        @DateUtc AS DateUtc, @RegionCode AS RegionCode, @PlaceId AS PlaceId
    ) AS src
    ON (tgt.DateUtc = src.DateUtc AND tgt.RegionCode = src.RegionCode
        AND ((tgt.PlaceId IS NULL AND src.PlaceId IS NULL) OR tgt.PlaceId = src.PlaceId)
        AND tgt.Active = 1)
    WHEN MATCHED THEN
        UPDATE SET
            EventName       = @EventName,
            ExpectedLevel   = @ExpectedLevel,
            Confidence      = @Confidence,
            StartLocalTime  = @StartLocalTime,
            EndLocalTime    = @EndLocalTime,
            LeadHours       = @LeadHours,
            MessageTemplate = @MessageTemplate,
            Tags            = @Tags,
            Active          = @Active
    WHEN NOT MATCHED THEN
        INSERT (DateUtc, RegionCode, PlaceId, EventName, ExpectedLevel, Confidence,
                StartLocalTime, EndLocalTime, LeadHours, MessageTemplate, Tags, Active)
        VALUES (@DateUtc, @RegionCode, @PlaceId, @EventName, @ExpectedLevel, @Confidence,
                @StartLocalTime, @EndLocalTime, @LeadHours, @MessageTemplate, @Tags, @Active);
END
GO