SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE dbo.sp_ArchivePastTrafficCondition
    @MaxAgeMinutes INT = 15
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TrafficCondition
    SET Active = 0
    WHERE Active = 1
      AND LastSeenAt < DATEADD(MINUTE, -@MaxAgeMinutes, SYSUTCDATETIME());

    SELECT @@ROWCOUNT AS ArchivedCount;
END;
GO























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.