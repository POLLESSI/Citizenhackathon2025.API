CREATE PROCEDURE [dbo].[sp_CrowdInfoAntennaSnapshot_BulkUpsert]
    @Rows dbo.CrowdInfoAntennaSnapshotTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE T WITH (UPDLOCK, HOLDLOCK)
       SET T.ActiveConnections = S.ActiveConnections,
           T.Confidence        = S.Confidence,
           T.Source            = S.Source,
           T.CreatedUtc        = SYSUTCDATETIME()
    FROM dbo.CrowdInfoAntennaSnapshot T
    INNER JOIN @Rows S
      ON  T.AntennaId      = S.AntennaId
      AND T.WindowStartUtc = S.WindowStartUtc
      AND T.WindowSeconds  = S.WindowSeconds;

    INSERT INTO dbo.CrowdInfoAntennaSnapshot
    (
        AntennaId,
        WindowStartUtc,
        WindowSeconds,
        ActiveConnections,
        Confidence,
        Source,
        CreatedUtc
    )
    SELECT
        S.AntennaId,
        S.WindowStartUtc,
        S.WindowSeconds,
        S.ActiveConnections,
        S.Confidence,
        S.Source,
        SYSUTCDATETIME()
    FROM @Rows S
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.CrowdInfoAntennaSnapshot T WITH (UPDLOCK, HOLDLOCK)
        WHERE T.AntennaId      = S.AntennaId
          AND T.WindowStartUtc = S.WindowStartUtc
          AND T.WindowSeconds  = S.WindowSeconds
    );

    COMMIT TRANSACTION;
END
GO























































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.