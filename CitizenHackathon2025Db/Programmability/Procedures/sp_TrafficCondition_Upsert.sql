CREATE PROCEDURE dbo.sp_TrafficCondition_Upsert
    @Latitude DECIMAL(9,6),
    @Longitude DECIMAL(9,6),
    @DateCondition DATETIME2(0),
    @CongestionLevel NVARCHAR(16),
    @IncidentType NVARCHAR(64),
    @Provider NVARCHAR(16),
    @ExternalId NVARCHAR(128),
    @Fingerprint VARBINARY(32),
    @LastSeenAt DATETIME2(0),
    @Title NVARCHAR(256) = NULL,
    @Road NVARCHAR(128) = NULL,
    @Severity TINYINT = NULL,
    @GeomWkt NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Id INT;

    UPDATE dbo.TrafficCondition
    SET
        Latitude = @Latitude,
        Longitude = @Longitude,
        DateCondition = @DateCondition,
        CongestionLevel = @CongestionLevel,
        IncidentType = @IncidentType,
        Fingerprint = @Fingerprint,
        LastSeenAt = @LastSeenAt,
        Title = @Title,
        Road = @Road,
        Severity = @Severity,
        GeomWkt = @GeomWkt,
        Active = 1
    WHERE Provider = @Provider
      AND ExternalId = @ExternalId;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.TrafficCondition
        (
            Latitude,
            Longitude,
            DateCondition,
            CongestionLevel,
            IncidentType,
            Provider,
            ExternalId,
            Fingerprint,
            LastSeenAt,
            Title,
            Road,
            Severity,
            GeomWkt,
            Active
        )
        VALUES
        (
            @Latitude,
            @Longitude,
            @DateCondition,
            @CongestionLevel,
            @IncidentType,
            @Provider,
            @ExternalId,
            @Fingerprint,
            @LastSeenAt,
            @Title,
            @Road,
            @Severity,
            @GeomWkt,
            1
        );

        SET @Id = CONVERT(INT, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        SELECT @Id = Id
        FROM dbo.TrafficCondition
        WHERE Provider = @Provider
          AND ExternalId = @ExternalId;
    END;

    SELECT
        Id,
        Latitude,
        Longitude,
        DateCondition,
        CongestionLevel,
        IncidentType,
        Provider,
        ExternalId,
        Fingerprint,
        LastSeenAt,
        Title,
        Road,
        Severity,
        GeomWkt,
        Active
    FROM dbo.TrafficCondition
    WHERE Id = @Id;
END;
GO












































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.