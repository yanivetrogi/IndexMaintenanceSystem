namespace IndexMaintenanceSystem.Migrations;

public static class _23_AlwaysonDatabasesAgName
{
    // ims_alwayson_databases is a transient tracking table (always empty after startup revert).
    // Rebuild it with a composite PK (database_id, ag_name) so that multiple AGs on the same
    // server are each tracked and reverted independently.
    public static string AlwaysonDatabasesAgNameSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ims_alwayson_databases' AND COLUMN_NAME = 'ag_name')
BEGIN
    DROP TABLE [dbo].[ims_alwayson_databases];

    CREATE TABLE [dbo].[ims_alwayson_databases] (
        database_id INT NOT NULL,
        ag_name     NVARCHAR(128) NOT NULL,
        CONSTRAINT PK_ims_alwayson_databases PRIMARY KEY (database_id, ag_name),
        FOREIGN KEY (database_id) REFERENCES [dbo].[ims_databases](database_id)
    );
END";
}
