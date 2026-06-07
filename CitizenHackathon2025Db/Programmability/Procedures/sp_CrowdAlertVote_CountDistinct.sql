CREATE PROCEDURE [dbo].[sp_CrowdAlertVote_CountDistinct]
(
    @ZoneKey NVARCHAR(64)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SinceUtc DATETIME2(3) =
        DATEADD(MINUTE, -5, SYSUTCDATETIME());

    SELECT
        COUNT(DISTINCT COALESCE(
            CAST(UserId AS NVARCHAR(64)),
            DeviceHash,
            IpHash
        )) AS DistinctReporterCount
    FROM dbo.CrowdAlertVote
    WHERE ZoneKey = @ZoneKey
      AND CreatedAtUtc >= @SinceUtc
      AND Active = 1;
END
GO