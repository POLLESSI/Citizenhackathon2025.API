CREATE PROCEDURE dbo.ArchiveAndDeleteExpiredAntennaConnections
    @TimeoutSeconds INT = 60,
    @BatchSize INT = 500
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIME2(3) = DATEADD(SECOND, -ABS(@TimeoutSeconds), SYSUTCDATETIME());

    DECLARE @ToArchive TABLE (Id BIGINT PRIMARY KEY);

    INSERT INTO @ToArchive(Id)
    SELECT TOP (@BatchSize) Id
    FROM dbo.CrowdInfoAntennaConnection WITH (READPAST, UPDLOCK, ROWLOCK)
    WHERE Active = 1
      AND LastSeenUtc < @Cutoff
    ORDER BY LastSeenUtc ASC;

    SELECT
        c.Id, c.AntennaId, c.EventId,
        c.DeviceHash, c.IpHash, c.MacHash,
        c.Source, c.SignalStrength, c.Band,
        c.FirstSeenUtc, c.LastSeenUtc, c.Rssi, c.AdditionalJson,
        1
    FROM dbo.CrowdInfoAntennaConnection c
    JOIN @ToArchive t ON t.Id = c.Id;

    -- delete (soft-delete via trigger)
    DELETE FROM dbo.CrowdInfoAntennaConnection
    WHERE Id IN (SELECT Id FROM @ToArchive);
END
GO