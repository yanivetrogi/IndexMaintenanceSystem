namespace IndexMaintenanceSystem.Migrations;

public static class _11_SchedulesReplanTrigger
{
    public static string CreateSchedulesReplanTriggerSql =
@$"CREATE TRIGGER [dbo].[trigger_ims_schedules_replan_on_insert_update]
ON [dbo].[ims_schedules]
AFTER INSERT, UPDATE
AS
BEGIN
    DECLARE @schedule_id INT, @server_id INT, @database_id INT, @index_id INT;

    IF NOT (UPDATE(freq_type) OR
        UPDATE(freq_interval) OR
        UPDATE(freq_subday_type) OR
        UPDATE(freq_subday_interval) OR
        UPDATE(freq_relative_interval) OR
        UPDATE(freq_recurrence_factor) OR
        UPDATE(active_start_date) OR
        UPDATE(active_end_date) OR
        UPDATE(active_start_time) OR
        UPDATE(active_end_time))
        RETURN;

    DECLARE inserted_cursor CURSOR LOCAL FOR
    SELECT nch.schedule_id, nch.server_id, nch.database_id, nch.index_id
    FROM ims_next_checks nch INNER JOIN inserted sch ON sch.schedule_id = nch.schedule_id

    OPEN inserted_cursor;
    FETCH NEXT FROM inserted_cursor INTO @schedule_id, @server_id, @database_id, @index_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC [dbo].[sp_ims_plan_next_check]
            @server_id,
            @database_id,
            @index_id,
            @schedule_id;

        FETCH NEXT FROM inserted_cursor INTO @schedule_id, @server_id, @database_id, @index_id;
    END;

    CLOSE inserted_cursor;
    DEALLOCATE inserted_cursor;
END;";
}
