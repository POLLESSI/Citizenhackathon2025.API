CREATE TABLE [dbo].[WeatherForecast]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [DateWeather] DATETIME2 NOT NULL,
    [TemperatureC] INT NOT NULL,
    [TemperatureF] INT NOT NULL,
    [Summary] NVARCHAR(256) NOT NULL,
    [RainfallMm] FLOAT NULL,
    [Humidity] INT NULL,
    [WindSpeedKmh] FLOAT NULL,
    [Active] BIT NOT NULL CONSTRAINT DF_WeatherForecast_Active DEFAULT(1),

    CONSTRAINT [PK_WeatherForecast] PRIMARY KEY CLUSTERED ([Id])
);



































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.