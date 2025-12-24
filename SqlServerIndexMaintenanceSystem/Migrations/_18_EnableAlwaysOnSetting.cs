namespace SqlServerIndexMaintenanceSystem.Migrations;


public static class _18_EnableAlwaysOnSetting
{
    public static string EnableAlwaysOnSettingSql =
@"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ims_servers' AND COLUMN_NAME = 'enable_always_on_check')
BEGIN
    ALTER TABLE [dbo].[ims_servers]
    ADD enable_always_on_check BIT NULL;
    ALTER TABLE [dbo].[ims_databases]
    ADD enable_always_on_check BIT NULL;

    ALTER TABLE [dbo].[ims_history_entries]
    ADD enable_always_on_check BIT NULL;
END";
}