namespace IndexMaintenanceSystem.Migrations;

public static class _04_IndexesTrigger
{
    public static string CreateIndexesTriggerSql =
@$"CREATE TRIGGER [dbo].[trigger_ims_indexes_insert_update]
ON [dbo].[ims_indexes]
AFTER INSERT, UPDATE
AS
BEGIN
    IF NOT UPDATE(schedule_id)
        RETURN;

    DECLARE
        @server_id INT,
        @database_id INT,
        @index_id INT,
        @schedule_id INT;

    DECLARE inserted_cursor CURSOR LOCAL FOR
    SELECT db.server_id, i.database_id, i.index_id, i.schedule_id
    FROM inserted i
    INNER JOIN [dbo].[ims_databases] db ON db.database_id = i.database_id;

    OPEN inserted_cursor;
    FETCH NEXT FROM inserted_cursor INTO @server_id, @database_id, @index_id, @schedule_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @schedule_id IS NOT NULL
        BEGIN
            -- delete next_check for the indexes of different schedules (in case, if the schedule has been changed)
            DELETE FROM [dbo].[ims_next_checks]
                WHERE
                    server_id = @server_id
                    AND database_id = @database_id
                    AND index_id = @index_id
                    AND schedule_id <> @schedule_id;
            -- set next_check for the index
            EXEC [dbo].[sp_ims_plan_next_check]
                @server_id,
                @database_id,
                @index_id,
                @schedule_id;
        END
        ELSE
        BEGIN
            -- delete next_check for the index
            DELETE FROM [dbo].[ims_next_checks]
                WHERE
                    server_id = @server_id
                    AND database_id = @database_id
                    AND index_id = @index_id;
        END;

        FETCH NEXT FROM inserted_cursor INTO @server_id, @database_id, @index_id, @schedule_id;
    END;

    CLOSE inserted_cursor;
    DEALLOCATE inserted_cursor;
END;";
}
