namespace IndexMaintenanceSystem.Migrations;

public static class _02_ServersTrigger
{
    public static string CreateServersTriggerSql =
@$"CREATE TRIGGER [dbo].[trigger_ims_servers_insert_update]
ON [dbo].[ims_servers]
AFTER INSERT, UPDATE
AS
BEGIN
    IF NOT (UPDATE(schedule_id) OR UPDATE(discover_databases))
        RETURN;

/* PSEUDOCODE
if schedule AND discover_databases
    delete next_check for the server of different schedules (in case, if the schedule has been changed)
    set next_check for the server
else
    delete next_check for the server

for children without their own schedule
    call the trigger to update next_check
*/

    DECLARE
        @server_id INT,
        @database_id INT = NULL,
        @index_id INT = NULL,
        @discover_databases BIT,
        @schedule_id INT;

    DECLARE inserted_cursor CURSOR LOCAL FOR
    SELECT server_id, discover_databases, schedule_id
    FROM inserted;

    OPEN inserted_cursor;
    FETCH NEXT FROM inserted_cursor INTO @server_id, @discover_databases, @schedule_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN

        IF @schedule_id IS NOT NULL AND @discover_databases = 1
        BEGIN
            -- delete next_check for the server of different schedules (in case, if the schedule has been changed)
            DELETE FROM [dbo].[ims_next_checks]
            WHERE
                server_id = @server_id
                AND database_id IS NULL
                AND index_id IS NULL
                AND (@schedule_id IS NULL OR schedule_id <> @schedule_id);

            -- set next_check for the server
            EXEC [dbo].[sp_ims_plan_next_check]
                @server_id,
                @database_id,
                @index_id,
                @schedule_id;
        END
        ELSE
        BEGIN
            -- delete next_check for the server
            DELETE FROM [dbo].[ims_next_checks]
                WHERE
                    server_id = @server_id
                    AND database_id IS NULL
                    AND index_id IS NULL;
        END;
        
        FETCH NEXT FROM inserted_cursor INTO @server_id, @discover_databases, @schedule_id;
    END;
    
    CLOSE inserted_cursor;
    DEALLOCATE inserted_cursor;

    -- for children without their own schedule
        -- call the trigger to update next_check
    UPDATE [dbo].[ims_databases]
    SET schedule_id = NULL
    WHERE
        schedule_id IS NULL
        AND server_id IN (SELECT server_id FROM inserted);
END;";
}
