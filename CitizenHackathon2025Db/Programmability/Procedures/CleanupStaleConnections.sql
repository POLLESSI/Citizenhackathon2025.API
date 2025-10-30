CREATE PROCEDURE [dbo].[CleanupStaleConnections]
    @staleSeconds INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CrowdInfoAntennaConnection
    SET Active = 0
    WHERE Active = 1
      AND LastSeenUtc <= DATEADD(SECOND, -@staleSeconds, SYSUTCDATETIME());
END;
GO