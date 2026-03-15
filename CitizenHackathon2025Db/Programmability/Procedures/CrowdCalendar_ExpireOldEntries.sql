CREATE PROCEDURE [dbo].[CrowdCalendar_ExpireOldEntries]
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.CrowdCalendar
    SET Active = 0
    WHERE Active = 1
      AND DateUtc < CAST(SYSUTCDATETIME() AS date);
END
GO