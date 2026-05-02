CREATE OR ALTER PROCEDURE dbo.sp_ArchivePastWeatherForecast
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WeatherForecast
    SET Active = 0
    WHERE Active = 1
      AND DateWeather < SYSUTCDATETIME();

    SELECT @@ROWCOUNT AS ArchivedCount;
END;
GO





































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.