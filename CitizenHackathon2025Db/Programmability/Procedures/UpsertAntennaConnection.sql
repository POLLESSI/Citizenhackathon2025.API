CREATE PROCEDURE [dbo].[UpsertAntennaConnection]
    @AntennaId INT,
    @DeviceHash BINARY(32),
    @IpHash BINARY(32) = NULL,
    @MacHash BINARY(32) = NULL,
    @Source TINYINT = 0,
    @SignalStrength SMALLINT = NULL,
    @Band NVARCHAR(16) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.CrowdInfoAntennaConnection AS target
    USING (SELECT @AntennaId AS AntennaId, @DeviceHash AS DeviceHash) AS src
    ON target.AntennaId = src.AntennaId AND target.DeviceHash = src.DeviceHash
    WHEN MATCHED THEN
        UPDATE SET
            IpHash = COALESCE(@IpHash, IpHash),
            MacHash = COALESCE(@MacHash, MacHash),
            Source = @Source,
            SignalStrength = @SignalStrength,
            Band = @Band,
            LastSeenUtc = SYSUTCDATETIME(),
            Active = 1
    WHEN NOT MATCHED THEN
        INSERT (AntennaId, DeviceHash, IpHash, MacHash, Source, SignalStrength, Band, FirstSeenUtc, LastSeenUtc, Active)
        VALUES (@AntennaId, @DeviceHash, @IpHash, @MacHash, @Source, @SignalStrength, @Band, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
END;

GO