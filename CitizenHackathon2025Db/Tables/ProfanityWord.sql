CREATE TABLE dbo.ProfanityWord
(
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    Word              NVARCHAR(200) NOT NULL,
    NormalizedWord    NVARCHAR(200) NOT NULL,
    LanguageCode      NVARCHAR(10) NOT NULL CONSTRAINT DF_ProfanityWord_LanguageCode DEFAULT('fr'),
    Weight            INT NOT NULL CONSTRAINT DF_ProfanityWord_Weight DEFAULT(1),
    IsRegex           BIT NOT NULL CONSTRAINT DF_ProfanityWord_IsRegex DEFAULT(0),
    Category          NVARCHAR(100) NULL,
    Active            BIT NOT NULL CONSTRAINT DF_ProfanityWord_Active DEFAULT(1),
    CreatedAtUtc      DATETIME2(3) NOT NULL CONSTRAINT DF_ProfanityWord_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc      DATETIME2(3) NULL
);
GO

CREATE UNIQUE INDEX UX_ProfanityWord_NormalizedWord_Active
ON dbo.ProfanityWord(NormalizedWord, LanguageCode)
WHERE Active = 1;
GO

CREATE INDEX IX_ProfanityWord_Active
ON dbo.ProfanityWord(Active, LanguageCode);
GO