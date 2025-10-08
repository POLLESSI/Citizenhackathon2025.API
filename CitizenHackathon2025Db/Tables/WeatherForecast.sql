CREATE TABLE [dbo].[WeatherForecast]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [DateWeather] DATETIME2 NOT NULL,
    [Latitude] DECIMAL(9,6) NULL,
	[Longitude] DECIMAL(9,6) NULL,
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


































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.