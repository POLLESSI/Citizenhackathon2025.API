CREATE TABLE [dbo].[EmergencyEscalationRequest]
(
	[Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
	[DisasterAlertId] BIGINT NOT NULL,
    [TargetService] NVARCHAR(32) NOT NULL, -- Police / Fire / Ambulance / Multi
    [Status] NVARCHAR(32) NOT NULL,        -- PendingOperatorReview / Sent / Cancelled
    [PayloadJson] NVARCHAR(MAX) NOT NULL,
    [CreatedAtUtc] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    [SentAtUtc] DATETIME2(3) NULL,
    [ReviewedByUserId] INT NULL
)
