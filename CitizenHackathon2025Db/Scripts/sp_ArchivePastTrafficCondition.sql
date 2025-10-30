SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.sp_ArchivePastTrafficCondition
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[TrafficCondition]
    SET [Active] = 0
    WHERE [Active] = 1
      AND [DateCondition] < DATEADD(DAY, -1, SYSUTCDATETIME());
END
GO























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.