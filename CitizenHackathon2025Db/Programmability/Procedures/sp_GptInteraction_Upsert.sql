CREATE PROCEDURE dbo.sp_GptInteraction_Upsert
    @Prompt      NVARCHAR(MAX),
    @PromptHash  NVARCHAR(64),
    @Response    NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    -- 1) Archive (soft-delete via trigger) the existing active of this PromptHash
    DELETE FROM dbo.GptInteractions WITH (ROWLOCK)
     WHERE Active = 1
       AND PromptHash = @PromptHash;

    -- 2) Inserts the new ACTIVE interaction
    INSERT INTO dbo.GptInteractions (Prompt, PromptHash, Response, CreatedAt, Active)
    VALUES (@Prompt, @PromptHash, @Response, SYSUTCDATETIME(), 1);

    DECLARE @NewId INT = SCOPE_IDENTITY();

    COMMIT;

    SELECT * FROM dbo.GptInteractions WHERE Id = @NewId;
END
GO



































































































































--// Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.