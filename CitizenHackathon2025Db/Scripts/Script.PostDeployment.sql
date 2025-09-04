-- Post-Deployment : Index (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_DateWeather'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_DateWeather]
    ON [dbo].[WeatherForecast]([DateWeather]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_TemperatureC'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_TemperatureC]
    ON [dbo.WeatherForecast]([TemperatureC]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_TemperatureF'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_TemperatureF]
    ON [dbo].[WeatherForecast]([TemperatureF]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes 
               WHERE name = N'IX_WeatherForecast_Summary'
                 AND object_id = OBJECT_ID(N'dbo.WeatherForecast'))
    CREATE NONCLUSTERED INDEX [IX_WeatherForecast_Summary]
    ON [dbo].[WeatherForecast]([Summary]);
GO

-- Post-Deployment : Trigger (idempotent)
IF OBJECT_ID(N'dbo.OnDeleteWeatherForecast', N'TR') IS NULL
    EXEC('CREATE TRIGGER [dbo].[OnDeleteWeatherForecast]
          ON [dbo].[WeatherForecast]
          INSTEAD OF DELETE
          AS
          BEGIN
              SET NOCOUNT ON;
              UPDATE WF SET Active = 0
              FROM dbo.WeatherForecast WF
              JOIN deleted d ON d.Id = WF.Id;
          END');
GO
