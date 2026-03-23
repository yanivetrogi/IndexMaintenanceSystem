CREATE PROCEDURE [dbo].[sp_ims_plan_next_check] 
(
    @server_id INT,
    @database_id INT,
    @index_id INT,
    @schedule_id INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
         @freq_type INT
        ,@freq_interval INT
        ,@freq_subday_type INT
        ,@freq_subday_interval INT
        ,@freq_relative_interval INT
        ,@freq_recurrence_factor INT
        ,@active_start_date INT
        ,@active_end_date INT
        ,@active_start_time INT
        ,@active_end_time INT
        ,@next_execution_date INT
        ,@next_execution_time INT
        ,@last_execution_date INT
        ,@last_execution_time INT;

    SELECT TOP 1 @last_execution_date = previous_execution_date,
                @last_execution_time = previous_execution_time
    FROM [dbo].[ims_next_checks]
    WHERE server_id = @server_id
        AND (database_id = @database_id OR (database_id IS NULL AND @database_id IS NULL))
        AND (index_id = @index_id OR (index_id IS NULL AND @index_id IS NULL))
        AND (schedule_id = @schedule_id OR (schedule_id IS NULL AND @schedule_id IS NULL))
    ORDER BY next_execution_date DESC, next_execution_time DESC;

    IF @schedule_id IS NULL
    BEGIN
        -- If no schedule is provided, then this is a one-time right-away check.
        -- If previous_execution_datetime is null - schedule for now
        -- otherwise - remain next_execution_datetime null to finalize the next_check
        if ([dbo].[ims_agent_datetime](@last_execution_date, @last_execution_time) IS NULL)
        BEGIN
            SET @next_execution_date = CONVERT(INT, FORMAT(CURRENT_TIMESTAMP, 'yyyyMMdd'));
            SET @next_execution_time = CONVERT(INT, FORMAT(CURRENT_TIMESTAMP, 'HHmmss'));
        END;
    END
    ELSE
    BEGIN
        SELECT @freq_type = freq_type,
            @freq_interval = freq_interval,
            @freq_subday_type = freq_subday_type,
            @freq_subday_interval = freq_subday_interval,
            @freq_relative_interval = freq_relative_interval,
            @freq_recurrence_factor = freq_recurrence_factor,
            @active_start_date = active_start_date,
            @active_end_date = active_end_date,
            @active_start_time = active_start_time,
            @active_end_time = active_end_time
        FROM [dbo].[ims_schedules]
        WHERE schedule_id = @schedule_id;

        EXEC [dbo].[sp_ims_get_schedule_next_execution_date_and_time]
            @freq_type
            ,@freq_interval
            ,@freq_subday_type
            ,@freq_subday_interval
            ,@freq_relative_interval
            ,@freq_recurrence_factor
            ,@active_start_date
            ,@active_end_date
            ,@active_start_time
            ,@active_end_time
            ,@last_execution_date
            ,@last_execution_time
            ,@next_execution_date OUTPUT
            ,@next_execution_time OUTPUT;
    END;
    
    UPDATE [dbo].[ims_next_checks]
    SET
        next_execution_date = @next_execution_date,
        next_execution_time = @next_execution_time
    WHERE server_id = @server_id 
    AND ((@database_id IS NULL AND database_id IS NULL) OR database_id = @database_id)
    AND ((@index_id IS NULL AND index_id IS NULL) OR index_id = @index_id)
    AND ((@schedule_id IS NULL and schedule_id IS NULL ) OR schedule_id = @schedule_id);

    IF @@ROWCOUNT = 0
        INSERT INTO [dbo].[ims_next_checks]
            (server_id, database_id, index_id, schedule_id, previous_execution_date, 
            previous_execution_time, next_execution_date, next_execution_time)
        VALUES
            (@server_id, @database_id, @index_id, @schedule_id, @last_execution_date,
            @last_execution_time, @next_execution_date, @next_execution_time);

    SELECT @server_id server_id, @database_id database_id, @index_id index_id, @schedule_id schedule_id, @last_execution_date previous_execution_date,
        @last_execution_time previous_execution_time, @next_execution_date next_execution_date, @next_execution_time next_execution_time;
END