CREATE TABLE [dbo].[WeatherForecast]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [DateWeather] DATETIME2 NOT NULL,
    [Latitude] DECIMAL(9,3) DEFAULT(50.890000),
	[Longitude] DECIMAL(9,3) DEFAULT(4.340000),
    [TemperatureC] INT NOT NULL,
    [TemperatureF] AS (CONVERT(int, (32 + (TemperatureC / 0.5556)))),
    [Summary] NVARCHAR(256) NOT NULL,
    [RainfallMm] FLOAT NULL,
    [Humidity] INT NULL,
    [WindSpeedKmh] FLOAT NULL,
    [Active] BIT NOT NULL CONSTRAINT DF_WeatherForecast_Active DEFAULT(1),

    CONSTRAINT [PK_WeatherForecast] PRIMARY KEY CLUSTERED ([Id])
);

GO

CREATE INDEX IX_WeatherForecast_Active_DateWeather
ON dbo.WeatherForecast (Active, DateWeather DESC);
GO

CREATE TRIGGER [dbo].[OnDeleteWeatherForecast]
ON [dbo].[WeatherForecast]
INSTEAD OF DELETE
AS
BEGIN
    UPDATE [dbo].[WeatherForecast]
    SET Active = 0
    WHERE Id IN (SELECT Id FROM deleted);
    
END;
GO

CREATE UNIQUE INDEX UX_WeatherForecast_Active_Date_Lat_Lon
ON dbo.WeatherForecast(DateWeather, Latitude, Longitude)
WHERE Active = 1;

GO

































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.