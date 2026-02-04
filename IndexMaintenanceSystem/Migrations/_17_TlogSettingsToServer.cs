namespace IndexMaintenanceSystem.Migrations;


public static class _17_TlogSettingsToServer
{
    public static string TlogSettingsToServerSql =
@"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ims_servers' AND COLUMN_NAME = 'disk_safety_pct')
BEGIN
    ALTER TABLE [dbo].[ims_servers]
    ADD disk_safety_pct INT NULL;
    ALTER TABLE [dbo].[ims_servers]
    ADD disk_min_remaining_mb INT NULL;
    ALTER TABLE [dbo].[ims_servers]
    ADD enable_tlog_disk_check BIT NULL;
    ALTER TABLE [dbo].[ims_servers]
    ADD enable_tlog_file_check BIT NULL;
    
    ALTER TABLE [dbo].[ims_history_entries]
    ADD disk_safety_pct INT NULL;
    ALTER TABLE [dbo].[ims_history_entries]
    ADD disk_min_remaining_mb INT NULL;
    ALTER TABLE [dbo].[ims_history_entries]
    ADD enable_tlog_disk_check BIT NULL;
    ALTER TABLE [dbo].[ims_history_entries]
    ADD enable_tlog_file_check BIT NULL;
END";
}
