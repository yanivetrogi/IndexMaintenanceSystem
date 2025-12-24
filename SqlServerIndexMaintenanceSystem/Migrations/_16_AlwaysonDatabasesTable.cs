namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _16_AlwaysonDatabasesTable
{
    public static string CreateAlwaysonDatabasesTableSql = @$"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME like 'ims_alwayson_databases')
BEGIN

    CREATE TABLE [dbo].[ims_alwayson_databases] (
        database_id INT PRIMARY KEY,
        FOREIGN KEY (database_id) REFERENCES [dbo].[ims_databases](database_id)
    );
END";
}