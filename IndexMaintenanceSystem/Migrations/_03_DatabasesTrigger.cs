namespace IndexMaintenanceSystem.Migrations;

public static class _03_DatabasesTrigger
{
    public static string CreateDatabasesTriggerSql =
@$"CREATE TRIGGER [dbo].[trigger_ims_databases_insert_update]
ON [dbo].[ims_databases]
AFTER INSERT, UPDATE
AS
BEGIN
    IF NOT UPDATE(schedule_id)
        RETURN;
        
/* PSEUDOCODE
schedule = db.schedule ?? server.schedule
if schedule
    delete next_check for the database of different schedules (in case, if the schedule has been changed)
    if schedule
        set next_check for the database with schedule
else
    delete next_checks for the database
*/
    DECLARE
        @server_id INT,
        @database_id INT,
        @index_id INT = NULL,
        @schedule_id INT;

    DECLARE inserted_cursor CURSOR LOCAL FOR
    SELECT db.server_id, db.database_id, ISNULL(db.schedule_id, s.schedule_id) schedule_id
    FROM inserted db
    LEFT JOIN [dbo].[ims_servers] s ON db.server_id = s.server_id;

    OPEN inserted_cursor;
    FETCH NEXT FROM inserted_cursor INTO @server_id, @database_id, @schedule_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @schedule_id IS NOT NULL
        BEGIN
            -- delete next_check for the database of different schedules (in case, if the schedule has been changed)
            DELETE FROM [dbo].[ims_next_checks]
            WHERE
                server_id = @server_id
                AND database_id = @database_id
                AND index_id IS NULL
                AND (@schedule_id IS NULL OR schedule_id IS NULL OR schedule_id <> @schedule_id);

            -- set next_check for the database
            EXEC [dbo].[sp_ims_plan_next_check]
                @server_id,
                @database_id,
                @index_id,
                @schedule_id;
        END
        ELSE
        BEGIN
            -- delete next_checks for the database
            DELETE FROM [dbo].[ims_next_checks]
            WHERE server_id = @server_id
                AND database_id = @database_id
                AND index_id IS NULL;
        END;

        FETCH NEXT FROM inserted_cursor INTO @server_id, @database_id, @schedule_id;
    END;

    CLOSE inserted_cursor;
    DEALLOCATE inserted_cursor;
END;
";
}
