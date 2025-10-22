SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.sp_ArchivePastWeatherForecast
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[CrowdInfo]
    SET [Active] = 0
    WHERE [Active] = 1
      AND [DateWeather] < DATEADD(DAY, -1, SYSUTCDATETIME());
END
GO