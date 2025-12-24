namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _15_IndexNameNullable
{
    public static string IndexNameNullableSql = @$"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ims_indexes' AND COLUMN_NAME = 'name' AND TABLE_SCHEMA = 'dbo' AND IS_NULLABLE = 'NO')
BEGIN
    ALTER TABLE [dbo].[ims_indexes]
    ALTER COLUMN [name] NVARCHAR(255);
END";
}