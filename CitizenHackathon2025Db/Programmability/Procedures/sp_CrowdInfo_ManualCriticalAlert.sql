CREATE PROCEDURE [dbo].[sp_CrowdInfo_ManualCriticalAlert]
    @PlaceId INT,
    @Reason NVARCHAR(256) = NULL,
    @Source NVARCHAR(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @LocationName NVARCHAR(64),
        @Latitude DECIMAL(9,6),
        @Longitude DECIMAL(9,6),
        @NowUtc DATETIME2(0);

    SET @NowUtc = SYSUTCDATETIME();

    SELECT
        @LocationName = [Name],
        @Latitude = [Latitude],
        @Longitude = [Longitude]
    FROM [dbo].[Place]
    WHERE [Id] = @PlaceId
      AND [Active] = 1;

    IF @LocationName IS NULL
    BEGIN
        RAISERROR('Active place not found.', 16, 1);
        RETURN;
    END;

    UPDATE [dbo].[CrowdInfo]
    SET
        [CrowdLevel] = 4,
        [Timestamp] = @NowUtc,
        [Active] = 1
    WHERE [LocationName] = @LocationName
      AND [Latitude] = @Latitude
      AND [Longitude] = @Longitude;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [dbo].[CrowdInfo]
        (
            [LocationName],
            [Latitude],
            [Longitude],
            [CrowdLevel],
            [Timestamp],
            [Active]
        )
        VALUES
        (
            @LocationName,
            @Latitude,
            @Longitude,
            4,
            @NowUtc,
            1
        );
    END;

    SELECT TOP (1)
        [Id],
        [LocationName],
        CAST([Latitude] AS FLOAT) AS [Latitude],
        CAST([Longitude] AS FLOAT) AS [Longitude],
        [CrowdLevel],
        [Timestamp]
    FROM [dbo].[CrowdInfo]
    WHERE [LocationName] = @LocationName
      AND [Latitude] = @Latitude
      AND [Longitude] = @Longitude
      AND [Active] = 1
    ORDER BY [Timestamp] DESC, [Id] DESC;
END