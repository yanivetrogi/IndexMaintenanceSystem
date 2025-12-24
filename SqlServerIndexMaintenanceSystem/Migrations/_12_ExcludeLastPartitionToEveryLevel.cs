namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _12_ExcludeLastPartitionToEveryLevel
{
    public static string ExcludeLastPartitionToEveryLevelSql = @$"
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ims_servers' AND COLUMN_NAME = 'exclude_last_partition' AND TABLE_SCHEMA = 'dbo')
    BEGIN
        ALTER TABLE [dbo].[ims_servers] ADD exclude_last_partition BIT NOT NULL DEFAULT 0;
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ims_databases' AND COLUMN_NAME = 'exclude_last_partition' AND TABLE_SCHEMA = 'dbo')
    BEGIN
        ALTER TABLE [dbo].[ims_databases] ADD exclude_last_partition BIT NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ims_indexes' AND COLUMN_NAME = 'exclude_last_partition' AND TABLE_SCHEMA = 'dbo')
    BEGIN
        ALTER TABLE [dbo].[ims_indexes] ADD exclude_last_partition BIT NULL;
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[ims_indexes] ALTER COLUMN exclude_last_partition BIT NULL;
    END

    EXEC sp_executesql N'
        UPDATE [dbo].[ims_indexes]
        SET exclude_last_partition = NULL
        where exclude_last_partition = 0;
    ';
END";
}
