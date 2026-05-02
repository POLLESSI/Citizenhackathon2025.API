SELECT SYSUTCDATETIME() AS NowUtc;

SELECT COUNT(*) AS ActiveFuture
FROM dbo.WeatherForecast
WHERE Active = 1
  AND DateWeather >= SYSUTCDATETIME();

SELECT COUNT(*) AS ActiveExpired
FROM dbo.WeatherForecast
WHERE Active = 1
  AND DateWeather < SYSUTCDATETIME();