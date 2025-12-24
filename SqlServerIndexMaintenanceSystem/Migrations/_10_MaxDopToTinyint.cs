namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _10_MaxDopToTinyint
{
    public static string AlterMaxdopFromBitToTinyintSql = @$"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME LIKE 'ims_%' AND COLUMN_NAME = 'maxdop' AND TABLE_SCHEMA = 'dbo' AND DATA_TYPE = 'bit')
BEGIN
    ALTER TABLE [dbo].[ims_servers]
    ALTER COLUMN maxdop TINYINT NULL;

    ALTER TABLE [dbo].[ims_databases]
    ALTER COLUMN maxdop TINYINT NULL;

    ALTER TABLE [dbo].[ims_indexes]
    ALTER COLUMN maxdop TINYINT NULL;

    ALTER TABLE [dbo].[ims_history_entries]
    ALTER COLUMN maxdop TINYINT NULL;
END
    ";
}
