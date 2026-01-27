CREATE PROCEDURE dbo.sp_AntennaConnection_Heartbeat
  @AntennaId int,
  @EventId int = NULL,
  @DeviceHash varbinary(32),
  @SeenUtc datetime2(3),
  @Rssi smallint = NULL
AS
BEGIN
  SET NOCOUNT ON;

  UPDATE dbo.CrowdInfoAntennaConnection
    SET LastSeenUtc = @SeenUtc,
        Rssi = COALESCE(@Rssi, Rssi),
        Active = 1
  WHERE AntennaId=@AntennaId AND
        ((EventId IS NULL AND @EventId IS NULL) OR EventId=@EventId) AND
        DeviceHash=@DeviceHash;

  IF @@ROWCOUNT = 0
  BEGIN
    INSERT dbo.CrowdInfoAntennaConnection(AntennaId, EventId, DeviceHash, FirstSeenUtc, LastSeenUtc, Rssi, Active)
    VALUES (@AntennaId, @EventId, @DeviceHash, @SeenUtc, @SeenUtc, @Rssi, 1);
  END
END
