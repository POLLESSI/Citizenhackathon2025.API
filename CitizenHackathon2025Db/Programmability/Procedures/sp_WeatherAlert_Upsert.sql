CREATE PROCEDURE dbo.sp_WeatherAlert_Upsert
    @Provider    NVARCHAR(16),
    @ExternalId  NVARCHAR(128),
    @Latitude    DECIMAL(9,3) = NULL,
    @Longitude   DECIMAL(9,3) = NULL,
    @SenderName  NVARCHAR(128) = NULL,
    @EventName   NVARCHAR(128) = NULL,
    @StartUtc    DATETIME2(0),
    @EndUtc      DATETIME2(0),
    @Description NVARCHAR(MAX) = NULL,
    @Tags        NVARCHAR(512) = NULL,
    @Severity    TINYINT = NULL,
    @LastSeenAt  DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.WeatherAlert AS T
    USING (SELECT @Provider AS Provider, @ExternalId AS ExternalId) AS S
      ON T.Provider = S.Provider AND T.ExternalId = S.ExternalId
    WHEN MATCHED THEN
      UPDATE SET
        Latitude    = @Latitude,
        Longitude   = @Longitude,
        SenderName  = @SenderName,
        EventName   = @EventName,
        StartUtc    = @StartUtc,
        EndUtc      = @EndUtc,
        Description = @Description,
        Tags        = @Tags,
        Severity    = @Severity,
        LastSeenAt  = @LastSeenAt,
        Active      = 1
    WHEN NOT MATCHED THEN
      INSERT (Provider, ExternalId, Latitude, Longitude, SenderName, EventName, StartUtc, EndUtc, Description, Tags, Severity, LastSeenAt, Active)
      VALUES (@Provider, @ExternalId, @Latitude, @Longitude, @SenderName, @EventName, @StartUtc, @EndUtc, @Description, @Tags, @Severity, @LastSeenAt, 1);

    SELECT TOP(1) * FROM dbo.WeatherAlert WHERE Provider=@Provider AND ExternalId=@ExternalId;
END;
GO

























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.