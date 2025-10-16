CREATE TABLE dbo.CrowdCalendar
(
    Id                 INT IDENTITY PRIMARY KEY,
    DateUtc            DATE               NOT NULL,         -- target day (UTC)
    RegionCode         NVARCHAR(32)       NOT NULL,         -- e.g.: “BE-WLX-NAM”, “Rochefort”, etc.
    PlaceId            INT                NULL,             -- if we target a specific site (Han Cave...)
    EventName          NVARCHAR(128)      NULL,             -- "Oktoberfest (flux DE)"
    ExpectedLevel      TINYINT            NOT NULL,         -- 1..4 => CrowdLevelEnum
    Confidence         TINYINT            NULL,             -- 0..100
    StartLocalTime     TIME(0)            NULL,             -- ex: 09:00 (optional)
    EndLocalTime       TIME(0)            NULL,
    LeadHours          INT                NOT NULL DEFAULT 3, -- warn Xh before StartLocalTime (or early in the morning)
    MessageTemplate    NVARCHAR(512)      NULL,             -- customizable text
    Tags               NVARCHAR(128)      NULL,             -- "family;festival;DE;FR;NL"
    Active             BIT                NOT NULL DEFAULT 1,
    CreatedAt          DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME()
);

GO

CREATE INDEX IX_CrowdCalendar_DateRegion
  ON dbo.CrowdCalendar(DateUtc, RegionCode);

GO

CREATE INDEX IX_CrowdCalendar_DatePlace
  ON dbo.CrowdCalendar(DateUtc, PlaceId);

GO

CREATE TRIGGER [dbo].[OnDeleteCrowdCalendar]
	ON [dbo].[CrowdCalendar]
	INSTEAD OF DELETE
	AS
	BEGIN
		UPDATE CrowdCalendar SET Active = 0
		WHERE Id IN (SELECT Id FROM deleted)
	END

GO

-- Unicité logique par jour/région/(place) sur les lignes actives
-- (permet des doublons historiques si Active=0)
CREATE UNIQUE INDEX UX_CrowdCalendar_DateRegionPlace_Active
ON dbo.CrowdCalendar(DateUtc, RegionCode, PlaceId, Active)
WHERE Active = 1;

GO
