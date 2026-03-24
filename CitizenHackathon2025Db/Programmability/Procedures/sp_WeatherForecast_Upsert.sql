CREATE PROCEDURE [dbo].[sp_WeatherForecast_Upsert]
    @DateWeather     DATETIME2,
    @Latitude        DECIMAL(9,6) = NULL,
    @Longitude       DECIMAL(9,6) = NULL,
    @TemperatureC    INT,
    @Summary         NVARCHAR(256),
    @RainfallMm      FLOAT = NULL,
    @Humidity        INT = NULL,
    @WindSpeedKmh    FLOAT = NULL,
    @WeatherMain     NVARCHAR(64) = NULL,
    @Description     NVARCHAR(256) = NULL,
    @Icon            NVARCHAR(16) = NULL,
    @IconUrl         NVARCHAR(256) = NULL,
    @WeatherType     INT = 0,
    @IsSevere        BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;

    SELECT TOP (1) @ExistingId = Id
    FROM dbo.WeatherForecast
    WHERE Active = 1
      AND DateWeather = @DateWeather
      AND ISNULL(Latitude, 0) = ISNULL(@Latitude, 0)
      AND ISNULL(Longitude, 0) = ISNULL(@Longitude, 0);

    IF @ExistingId IS NOT NULL
    BEGIN
        UPDATE WF
        SET TemperatureC = @TemperatureC,
            Summary = @Summary,
            RainfallMm = @RainfallMm,
            Humidity = @Humidity,
            WindSpeedKmh = @WindSpeedKmh,
            WeatherMain = @WeatherMain,
            Description = @Description,
            Icon = @Icon,
            IconUrl = @IconUrl,
            WeatherType = @WeatherType,
            IsSevere = @IsSevere,
            Active = 1
        OUTPUT INSERTED.*
        FROM dbo.WeatherForecast WF
        WHERE WF.Id = @ExistingId;
    END
    ELSE
    BEGIN
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
            WeatherMain,
            Description,
            Icon,
            IconUrl,
            WeatherType,
            IsSevere,
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
            @WeatherMain,
            @Description,
            @Icon,
            @IconUrl,
            @WeatherType,
            @IsSevere,
            1
        );
    END
END
GO











































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.