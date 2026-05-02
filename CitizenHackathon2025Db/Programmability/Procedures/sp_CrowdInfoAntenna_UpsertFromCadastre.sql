CREATE PROCEDURE dbo.sp_CrowdInfoAntenna_UpsertFromCadastre
    @ExternalSource NVARCHAR(32),
    @ExternalId NVARCHAR(128),
    @Name NVARCHAR(64),
    @Latitude DECIMAL(9,6),
    @Longitude DECIMAL(9,6),
    @Description NVARCHAR(256) = NULL,
    @MaxCapacity INT = NULL,
    @Active BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM dbo.CrowdInfoAntenna
        WHERE ExternalSource = @ExternalSource
          AND ExternalId = @ExternalId
    )
    BEGIN
        UPDATE dbo.CrowdInfoAntenna
        SET
            Name = @Name,
            Latitude = @Latitude,
            Longitude = @Longitude,
            Description = @Description,
            MaxCapacity = @MaxCapacity,
            Active = @Active,
            LastSyncedUtc = SYSUTCDATETIME()
        WHERE ExternalSource = @ExternalSource
          AND ExternalId = @ExternalId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CrowdInfoAntenna
        (
            Name,
            Latitude,
            Longitude,
            Description,
            MaxCapacity,
            Active,
            ExternalSource,
            ExternalId,
            LastSyncedUtc
        )
        VALUES
        (
            @Name,
            @Latitude,
            @Longitude,
            @Description,
            @MaxCapacity,
            @Active,
            @ExternalSource,
            @ExternalId,
            SYSUTCDATETIME()
        );
    END;

    SELECT TOP(1)
        Id,
        Name,
        Latitude,
        Longitude,
        Active,
        CreatedUtc,
        Description,
        MaxCapacity,
        ExternalSource,
        ExternalId,
        LastSyncedUtc
    FROM dbo.CrowdInfoAntenna
    WHERE ExternalSource = @ExternalSource
      AND ExternalId = @ExternalId;
END;
GO




















































































































-- Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.