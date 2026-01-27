/* =========================================================
   CrowdInfoAntennaSnapshot.sql
   - Aggregated snapshots (0 identifiers)
   - Target window 10s (WindowSeconds)
   - Bulk upsert via TVP + stored procedure
   - Purge history
   ========================================================= */

CREATE TABLE [dbo].[CrowdInfoAntennaSnapshot]
(
    [AntennaId]         INT           NOT NULL,
    [WindowStartUtc]    DATETIME2(3)   NOT NULL,
    [WindowSeconds]     SMALLINT      NOT NULL CONSTRAINT [DF_CIAS_WindowSeconds] DEFAULT (10),

    [ActiveConnections] INT           NOT NULL,
    [Confidence]        TINYINT       NOT NULL CONSTRAINT [DF_CIAS_Confidence]     DEFAULT (80),
    [Source]            TINYINT       NOT NULL CONSTRAINT [DF_CIAS_Source]         DEFAULT (1),

    [CreatedUtc]        DATETIME2(3)  NOT NULL CONSTRAINT [DF_CIAS_CreatedUtc]     DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_CrowdInfoAntennaSnapshot]
        PRIMARY KEY CLUSTERED ([AntennaId], [WindowStartUtc], [WindowSeconds]),

    CONSTRAINT [FK_CIAS_Antenna]
        FOREIGN KEY ([AntennaId]) REFERENCES [dbo].[CrowdInfoAntenna]([Id])
);

GO

CREATE INDEX [IX_CIAS_WindowStartUtc]
ON [dbo].[CrowdInfoAntennaSnapshot] ([WindowStartUtc] DESC, [WindowSeconds])
INCLUDE ([AntennaId], [ActiveConnections], [Confidence], [Source], [CreatedUtc]);

GO