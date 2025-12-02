CREATE PROCEDURE [dbo].[sp_WeatherForecast_Upsert]
    @DateWeather     DATETIME2,
    @Latitude        DECIMAL(9,6) = NULL,
    @Longitude       DECIMAL(9,6) = NULL,
    @TemperatureC    INT,
    @Summary         NVARCHAR(256),
    @RainfallMm      FLOAT = NULL,
    @Humidity        INT   = NULL,
    @WindSpeedKmh    FLOAT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;

    -- We are looking for an active line for the same date and the same coordinates
    SELECT TOP (1) @ExistingId = Id
    FROM dbo.WeatherForecast
    WHERE Active = 1
      AND DateWeather = @DateWeather
      AND ISNULL(Latitude, 0)  = ISNULL(@Latitude, 0)
      AND ISNULL(Longitude, 0) = ISNULL(@Longitude, 0);

    IF @ExistingId IS NOT NULL
    BEGIN
        -- UPDATE + OUTPUT so that Dapper can retrieve the updated line
        UPDATE WF
        SET TemperatureC = @TemperatureC,
            Summary      = @Summary,
            RainfallMm   = @RainfallMm,
            Humidity     = @Humidity,
            WindSpeedKmh = @WindSpeedKmh
        OUTPUT INSERTED.*
        FROM dbo.WeatherForecast WF
        WHERE WF.Id = @ExistingId;
    END
    ELSE
    BEGIN
        -- INSERT + OUTPUT so that Dapper can retrieve the new line
        INSERT INTO dbo.WeatherForecast
        (
            DateWeather,
            Latitude,
            Longitude,
            TemperatureC,
            Summary,
            RainfallMm,
            Humidity,
            WindSpeedKmh,
            Active
        )
        OUTPUT INSERTED.*
        VALUES
        (
            @DateWeather,
            @Latitude,
            @Longitude,
            @TemperatureC,
            @Summary,
            @RainfallMm,
            @Humidity,
            @WindSpeedKmh,
            1
        );
    END
END
GO











































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.