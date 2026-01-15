CREATE PROCEDURE dbo.sp_TrafficCondition_Upsert
    @Latitude        DECIMAL(9, 6),
    @Longitude       DECIMAL(9, 6),
    @DateCondition   DATETIME2(0),
    @CongestionLevel NVARCHAR(16),
    @IncidentType    NVARCHAR(64),
    @Provider        NVARCHAR(16),
    @ExternalId      NVARCHAR(128),
    @Fingerprint     VARBINARY(32),
    @LastSeenAt      DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    IF @LastSeenAt IS NULL SET @LastSeenAt = SYSUTCDATETIME();

    MERGE dbo.TrafficCondition AS T
    -- 1) Archive any existing active row for this location
    USING (SELECT @Provider AS Provider, @ExternalId AS ExternalId) AS S
      ON T.Provider = S.Provider AND T.ExternalId = S.ExternalId
    WHEN MATCHED THEN
      UPDATE SET
        Latitude        = @Latitude,
        Longitude       = @Longitude,
        DateCondition   = @DateCondition,
        CongestionLevel = @CongestionLevel,
        IncidentType    = @IncidentType,
        Fingerprint     = @Fingerprint,
        LastSeenAt      = @LastSeenAt,
        Active          = 1
    WHEN NOT MATCHED THEN
      INSERT (Latitude, Longitude, DateCondition, CongestionLevel, IncidentType, Provider, ExternalId, Fingerprint, LastSeenAt, Active)
      VALUES (@Latitude, @Longitude, @DateCondition, @CongestionLevel, @IncidentType, @Provider, @ExternalId, @Fingerprint, @LastSeenAt, 1);

    SELECT TOP (1) *
    FROM dbo.TrafficCondition
    WHERE Provider = @Provider AND ExternalId = @ExternalId;

END
GO













































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.