namespace IndexMaintenanceSystem.Migrations;


public static class _14_TlogFactors
{
    public static string TlogFactorsColumnsSql =
@"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME like 'ims_%' AND COLUMN_NAME = 'tlog_size_factor')
BEGIN
    ALTER TABLE [dbo].[ims_servers]
    ADD tlog_size_factor FLOAT NULL;
    ALTER TABLE [dbo].[ims_servers]
    ADD tlog_growth_factor FLOAT NULL;
    
    ALTER TABLE [dbo].[ims_databases]
    ADD tlog_size_factor FLOAT NULL;
    ALTER TABLE [dbo].[ims_databases]
    ADD tlog_growth_factor FLOAT NULL;
    
    ALTER TABLE [dbo].[ims_indexes]
    ADD tlog_size_factor FLOAT NULL;
    ALTER TABLE [dbo].[ims_indexes]
    ADD tlog_growth_factor FLOAT NULL;
    
    ALTER TABLE [dbo].[ims_history_entries]
    ADD tlog_size_factor FLOAT NULL;
    ALTER TABLE [dbo].[ims_history_entries]
    ADD tlog_growth_factor FLOAT NULL;
END";
}
