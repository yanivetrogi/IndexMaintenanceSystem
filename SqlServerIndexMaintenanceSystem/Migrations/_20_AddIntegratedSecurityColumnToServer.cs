namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _20_AddIntegratedSecurityColumnToServer
{
    public static string AddIntegratedSecurityColumnToServerSql =
@"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ims_servers' AND COLUMN_NAME = 'integrated_security')
BEGIN
    ALTER TABLE [dbo].[ims_servers]
    ADD integrated_security BIT NOT NULL DEFAULT 1;
END";
}