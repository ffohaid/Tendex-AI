SET NOCOUNT ON;

DECLARE @DatabaseName SYSNAME;
DECLARE @Sql NVARCHAR(MAX);

DECLARE database_cursor CURSOR FAST_FORWARD FOR
SELECT [name]
FROM sys.databases
WHERE database_id > 4
  AND state_desc = 'ONLINE';

OPEN database_cursor;
FETCH NEXT FROM database_cursor INTO @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'USE ' + QUOTENAME(@DatabaseName) + N';

IF OBJECT_ID(N''[rfp].[Competitions]'', N''U'') IS NOT NULL
BEGIN
    IF COL_LENGTH(N''rfp.Competitions'', N''RequiredAttachmentTypes'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [RequiredAttachmentTypes] NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''Department'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [Department] NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''FiscalYear'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [FiscalYear] NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''InquiryPeriodDays'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [InquiryPeriodDays] INT NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''InquiriesStartDate'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [InquiriesStartDate] DATETIME2 NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''OffersStartDate'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [OffersStartDate] DATETIME2 NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''ExpectedAwardDate'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [ExpectedAwardDate] DATETIME2 NULL;

    IF COL_LENGTH(N''rfp.Competitions'', N''WorkStartDate'') IS NULL
        ALTER TABLE [rfp].[Competitions] ADD [WorkStartDate] DATETIME2 NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N''rfp''
          AND t.name = N''Competitions''
          AND c.name = N''ReferenceNumber''
          AND c.is_nullable = 0
    )
        ALTER TABLE [rfp].[Competitions] ALTER COLUMN [ReferenceNumber] NVARCHAR(50) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N''rfp''
          AND t.name = N''Competitions''
          AND c.name = N''StartDate''
          AND c.is_nullable = 0
    )
        ALTER TABLE [rfp].[Competitions] ALTER COLUMN [StartDate] DATETIME2 NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N''rfp''
          AND t.name = N''Competitions''
          AND c.name = N''EndDate''
          AND c.is_nullable = 0
    )
        ALTER TABLE [rfp].[Competitions] ALTER COLUMN [EndDate] DATETIME2 NULL;
END;

IF OBJECT_ID(N''[evaluation].[SupplierOffers]'', N''U'') IS NOT NULL
BEGIN
    IF COL_LENGTH(N''evaluation.SupplierOffers'', N''DeletedAt'') IS NULL
        ALTER TABLE [evaluation].[SupplierOffers] ADD [DeletedAt] DATETIME2 NULL;

    IF COL_LENGTH(N''evaluation.SupplierOffers'', N''DeletedBy'') IS NULL
        ALTER TABLE [evaluation].[SupplierOffers] ADD [DeletedBy] NVARCHAR(450) NULL;
END;';

    BEGIN TRY
        EXEC (@Sql);
    END TRY
    BEGIN CATCH
        PRINT N'Compatibility script failed in database [' + REPLACE(@DatabaseName, '''', '''''') + N']: ' + ERROR_MESSAGE();
    END CATCH;

    FETCH NEXT FROM database_cursor INTO @DatabaseName;
END;

CLOSE database_cursor;
DEALLOCATE database_cursor;
