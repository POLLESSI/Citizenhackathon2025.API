CREATE PROCEDURE [dbo].[sp_CrowdInfoAntennaSnapshot_Purge]
    @KeepHours INT = 168
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cut DATETIME2(3) = DATEADD(HOUR, -@KeepHours, SYSUTCDATETIME());

    DELETE FROM [dbo].[CrowdInfoAntennaSnapshot]
    WHERE [WindowStartUtc] < @cut;
END
GO
