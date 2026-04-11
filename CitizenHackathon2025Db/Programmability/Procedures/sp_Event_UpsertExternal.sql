--IF OBJECT_ID(N'dbo.sp_Event_UpsertExternal', N'P') IS NULL
--    EXEC(N'CREATE PROCEDURE dbo.sp_Event_UpsertExternal AS BEGIN SET NOCOUNT ON; END');
--GO

CREATE PROCEDURE dbo.sp_Event_UpsertExternal
    @ExternalSource NVARCHAR(32),
    @PlaceExternalId NVARCHAR(128),
    @Name NVARCHAR(64),
    @Type NVARCHAR(32) = NULL,
    @Indoor BIT = NULL,
    @Latitude DECIMAL(9,6) = NULL,
    @Longitude DECIMAL(9,6) = NULL,
    @Capacity INT = NULL,
    @Tag NVARCHAR(16) = NULL,
    @Active BIT = 1,
    @SourceUpdatedAtUtc DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.Place AS tgt
    USING
    (
        SELECT
            @ExternalSource AS ExternalSource,
            @PlaceExternalId AS ExternalId
    ) AS src
    ON tgt.ExternalSource = src.ExternalSource
       AND tgt.ExternalId = src.ExternalId
    WHEN MATCHED THEN
        UPDATE SET
            Name = @Name,
            [Type] = @Type,
            Indoor = ISNULL(@Indoor, tgt.Indoor),
            Latitude = ISNULL(@Latitude, tgt.Latitude),
            Longitude = ISNULL(@Longitude, tgt.Longitude),
            Capacity = ISNULL(@Capacity, tgt.Capacity),
            Tag = @Tag,
            Active = @Active,
            SourceUpdatedAtUtc = @SourceUpdatedAtUtc
    WHEN NOT MATCHED THEN
        INSERT (Name, [Type], Indoor, Latitude, Longitude, Capacity, Tag, Active, ExternalSource, ExternalId, SourceUpdatedAtUtc)
        VALUES (@Name, @Type, @Indoor, @Latitude, @Longitude, @Capacity, @Tag, @Active, @ExternalSource, @PlaceExternalId, @SourceUpdatedAtUtc);

    SELECT TOP(1) *
    FROM dbo.Place
    WHERE ExternalSource = @ExternalSource
      AND ExternalId = @PlaceExternalId;
END;
GO






















































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.