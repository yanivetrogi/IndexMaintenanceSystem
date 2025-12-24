namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _13_IndexMinSize
{
    public static string IndexMinSizeSql = @$"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME like 'ims_%' AND COLUMN_NAME = 'index_min_size_kb')
BEGIN
    ALTER TABLE [dbo].[ims_servers]
    ADD index_min_size_kb INT NULL;
    
    ALTER TABLE [dbo].[ims_databases]
    ADD index_min_size_kb INT NULL;
    
    ALTER TABLE [dbo].[ims_indexes]
    ADD index_min_size_kb INT NULL;
    
    ALTER TABLE [dbo].[ims_history_entries]
    ADD index_min_size_kb INT NULL;
END";
}