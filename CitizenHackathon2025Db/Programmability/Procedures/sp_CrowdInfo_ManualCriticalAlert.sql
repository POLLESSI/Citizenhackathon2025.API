CREATE PROCEDURE [dbo].[sp_CrowdInfo_ManualCriticalAlert]
    @PlaceId INT,
    @Reason NVARCHAR(256) = NULL,
    @Source NVARCHAR(32) = N'ManualButton'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @LocationName NVARCHAR(64),
        @Latitude DECIMAL(9,6),
        @Longitude DECIMAL(9,6),
        @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @LocationName = [Name],
        @Latitude = [Latitude],
        @Longitude = [Longitude]
    FROM [dbo].[Place]
    WHERE [Id] = @PlaceId
      AND [Active] = 1;

    IF @LocationName IS NULL
        THROW 50001, 'Place not found or inactive.', 1;

    INSERT INTO [dbo].[CrowdInfo]
    (
        [LocationName],
        [Latitude],
        [Longitude],
        [CrowdLevel],
        [Timestamp],
        [Active],
        [IsManualCriticalAlert],
        [ExpiresAtUtc],
        [Source],
        [Reason]
    )
    VALUES
    (
        LEFT(CONCAT(N'FULL ALERT - ', @LocationName), 64),
        @Latitude,
        @Longitude,
        4,
        @NowUtc,
        1,
        1,
        DATEADD(MINUTE, 5, @NowUtc),
        @Source,
        @Reason
    );

    SELECT TOP (1)
        [Id],
        [LocationName],
        CAST([Latitude] AS FLOAT) AS [Latitude],
        CAST([Longitude] AS FLOAT) AS [Longitude],
        [CrowdLevel],
        [Timestamp],
        [Active],
        [IsManualCriticalAlert],
        [ExpiresAtUtc],
        [Source],
        [Reason]
    FROM [dbo].[CrowdInfo]
    WHERE [Id] = SCOPE_IDENTITY();
END;