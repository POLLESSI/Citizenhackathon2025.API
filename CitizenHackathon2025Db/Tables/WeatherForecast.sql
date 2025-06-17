CREATE TABLE [dbo].[WeatherForecast]
(
	[Id] INT IDENTITY,
	[DateWeather] DATETIME ,
	[TemperatureC] INT,
	[TemperatureF] INT,
	[Summary] NVARCHAR,
	[RainfallNm] FLOAT,
	[Humidity] INT,
	[WindSpeedKmh] FLOAT,
	[Active] BIT DEFAULT 1

	CONSTRAINT [PK_WeatherForecast] PRIMARY KEY ([Id]),
	CONSTRAINT [UQ_WeatherForecast_DateWeather] UNIQUE ([DateWeather]),
	CONSTRAINT [UQ_WeatherForecast_TemperatureC] UNIQUE ([TemperatureC]),
	CONSTRAINT [UQ_WeatherForecast_TemperatureF] UNIQUE ([TemperatureF]),
	CONSTRAINT [UQ_WeatherForecast_Summary] UNIQUE ([Summary])

)

GO

CREATE TRIGGER [dbo].[OnDeleteWeatherForecast]
    ON [dbo].[WeatherForecast]
	INSTEAD OF DELETE
	AS 
	BEGIN
		UPDATE WeatherForecast SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END




































































	--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.