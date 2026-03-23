SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.GptInteractions', N'U') IS NOT NULL
BEGIN
    DECLARE @uniq sysname;

    SELECT TOP (1) @uniq = i.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
       AND ic.index_id  = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.GptInteractions')
      AND i.is_unique = 1
      AND i.has_filter = 0
      AND c.name = N'PromptHash';

    IF @uniq IS NOT NULL
        EXEC(N'DROP INDEX [' + @uniq + N'] ON dbo.GptInteractions;');

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_GptInteractions_Active'
          AND object_id = OBJECT_ID(N'dbo.GptInteractions')
    )
    BEGIN
        CREATE INDEX [IX_GptInteractions_Active]
            ON dbo.GptInteractions([Active]);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_GptInteractions_Active_PromptHash'
          AND object_id = OBJECT_ID(N'dbo.GptInteractions')
    )
    BEGIN
        CREATE UNIQUE INDEX [UX_GptInteractions_Active_PromptHash]
            ON dbo.GptInteractions([PromptHash])
            WHERE [Active] = 1;
    END;
END;
GO