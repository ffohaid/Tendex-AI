SET NOCOUNT ON;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'
USE ' + QUOTENAME(name) + N';
IF EXISTS (
    SELECT 1
    FROM sys.schemas s
    INNER JOIN sys.tables t ON t.schema_id = s.schema_id
    WHERE s.name = ''rfp'' AND t.name = ''Competitions''
)
AND COL_LENGTH(''rfp.Competitions'', ''RequiredAttachmentTypes'') IS NULL
BEGIN
    PRINT ''Adding [rfp].[Competitions].[RequiredAttachmentTypes] in database: ' + REPLACE(name, '''', '''''') + ''';
    ALTER TABLE [rfp].[Competitions]
        ADD [RequiredAttachmentTypes] NVARCHAR(MAX) NULL;
END;
'
FROM sys.databases
WHERE database_id > 4
  AND state_desc = 'ONLINE';

EXEC sp_executesql @sql;
