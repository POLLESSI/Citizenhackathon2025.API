USE [CitizenHackathon2025Db];
GO

DROP PROCEDURE IF EXISTS dbo.sp_ArchivePastCrowdInfo;
GO

CREATE PROCEDURE dbo.sp_ArchivePastCrowdInfo
    @MaxAgeMinutes INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.CrowdInfo
    SET Active = 0
    WHERE Active = 1
      AND [Timestamp] < DATEADD(MINUTE, -@MaxAgeMinutes, SYSUTCDATETIME());

    SELECT @@ROWCOUNT AS ArchivedCount;
END;
GO
















































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.