CREATE PROCEDURE dbo.sp_WeatherForecast_Upsert
    @DateWeather    DATETIME2,
    @Latitude       DECIMAL(9,6),
    @Longitude      DECIMAL(9,6),
    @TemperatureC   INT,
    @Summary        NVARCHAR(256),
    @RainfallMm     FLOAT          = NULL,
    @Humidity       INT            = NULL,
    @WindSpeedKmh   FLOAT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WeatherForecast
       SET Active = 0
     WHERE Active = 1
       AND DateWeather = @DateWeather
       AND Latitude    = @Latitude
       AND Longitude   = @Longitude;

    INSERT INTO dbo.WeatherForecast
        (DateWeather, Latitude, Longitude, TemperatureC, Summary, RainfallMm, Humidity, WindSpeedKmh, Active)
    OUTPUT INSERTED.*
    VALUES
        (@DateWeather, @Latitude, @Longitude, @TemperatureC, @Summary, @RainfallMm, @Humidity, @WindSpeedKmh, 1);
END
GO








































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.