CREATE PROCEDURE dbo.sp_CrowdInfo_Upsert
    @LocationName NVARCHAR(64),
    @Latitude     DECIMAL(9,6),
    @Longitude    DECIMAL(9,6),
    @CrowdLevel   INT,
    @Timestamp    DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DELETE FROM dbo.CrowdInfo WITH (ROWLOCK)
    WHERE Active = 1
      AND LocationName = @LocationName
      AND Latitude = @Latitude
      AND Longitude = @Longitude;

    INSERT INTO dbo.CrowdInfo
    (
        LocationName,
        Latitude,
        Longitude,
        CrowdLevel,
        [Timestamp],
        Active
    )
    VALUES
    (
        @LocationName,
        @Latitude,
        @Longitude,
        @CrowdLevel,
        @Timestamp,
        1
    );

    DECLARE @NewId INT = CONVERT(INT, SCOPE_IDENTITY());

    COMMIT;

    SELECT
        Id,
        LocationName,
        Latitude,
        Longitude,
        CrowdLevel,
        [Timestamp],
        Active
    FROM dbo.CrowdInfo
    WHERE Id = @NewId;
END;
GO


















































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.