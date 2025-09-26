MERGE [dbo].[GptInteractions] WITH (HOLDLOCK) AS t
USING (SELECT @PromptHash AS PromptHash) AS s
ON (t.PromptHash = s.PromptHash)
WHEN MATCHED THEN
    UPDATE SET
        Response  = @Response,
        CreatedAt = SYSUTCDATETIME(),
        Active    = 1
WHEN NOT MATCHED THEN
    INSERT (Prompt, PromptHash, Response, CreatedAt, Active)
    VALUES (@Prompt, @PromptHash, @Response, SYSUTCDATETIME(), 1);