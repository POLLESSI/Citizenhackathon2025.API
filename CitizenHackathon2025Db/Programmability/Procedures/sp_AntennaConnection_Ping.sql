CREATE PROCEDURE [dbo].[sp_AntennaConnection_Ping]
	 @AntennaId       INT,
    @EventId         INT = NULL,
    @DeviceHash      BINARY(32),
    @IpHash          BINARY(32) = NULL,
    @MacHash         BINARY(32) = NULL,
    @Source          TINYINT = 0,
    @SignalStrength  SMALLINT = NULL,
    @Band            NVARCHAR(16) = NULL,
    @AdditionalJson  NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    UPDATE dbo.CrowdInfoAntennaConnection WITH (UPDLOCK, HOLDLOCK)
    SET
        LastSeenUtc    = @NowUtc,
        Active         = 1,
        IpHash         = COALESCE(@IpHash, IpHash),
        MacHash        = COALESCE(@MacHash, MacHash),
        Source         = @Source,
        SignalStrength = @SignalStrength,
        Band           = @Band,
        AdditionalJson = @AdditionalJson
    WHERE AntennaId = @AntennaId
      AND EventIdKey = ISNULL(@EventId, -1)
      AND DeviceHash = @DeviceHash;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.CrowdInfoAntennaConnection
        (
            AntennaId,
            EventId,
            DeviceHash,
            IpHash,
            MacHash,
            Source,
            SignalStrength,
            Band,
            FirstSeenUtc,
            LastSeenUtc,
            Rssi,
            Active,
            AdditionalJson
        )
        VALUES
        (
            @AntennaId,
            @EventId,
            @DeviceHash,
            @IpHash,
            @MacHash,
            @Source,
            @SignalStrength,
            @Band,
            @NowUtc,
            @NowUtc,
            NULL,
            1,
            @AdditionalJson
        );
    END;

    COMMIT TRANSACTION;

    SELECT
        @AntennaId AS AntennaId,
        @EventId AS EventId,
        @NowUtc AS SeenAtUtc;
END
GO

CREATE INDEX IX_AntennaConnection_Active_Antenna_LastSeen
ON dbo.CrowdInfoAntennaConnection(AntennaId, Active, LastSeenUtc DESC)
INCLUDE (EventId, DeviceHash, Source, SignalStrength, Band);
GO

CREATE INDEX IX_AntennaConnection_Active_LastSeen
ON dbo.CrowdInfoAntennaConnection(Active, LastSeenUtc ASC)
INCLUDE (AntennaId, EventId);
GO