ALTER TABLE dbo.CrowdInfoAntennaConnection
DROP CONSTRAINT DF_CIACD_DeletedUtc;

ALTER TABLE dbo.CrowdInfoAntennaConnection
DROP CONSTRAINT DF_CIACD_DeletedReason;

ALTER TABLE dbo.CrowdInfoAntennaConnection
ALTER COLUMN DeletedUtc DATETIME2(3) NULL;

ALTER TABLE dbo.CrowdInfoAntennaConnection
ALTER COLUMN DeletedReason TINYINT NULL;

UPDATE dbo.CrowdInfoAntennaConnection
SET DeletedUtc = NULL,
    DeletedReason = NULL
WHERE Active = 1;