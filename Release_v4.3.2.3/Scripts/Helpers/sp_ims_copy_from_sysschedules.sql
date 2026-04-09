/*
 * sp_ims_copy_from_sysschedules
 *
 * Copies a SQL Server Agent schedule from msdb.dbo.sysschedules into
 * dbo.ims_schedules, so it can be used as an IMS maintenance schedule.
 *
 * Parameters:
 *   @sysschedule_id  - The schedule_id of the existing SQL Agent schedule in msdb.
 *   @schedule_id     - OUTPUT: The new schedule_id inserted into ims_schedules.
 *
 * Usage:
 *   DECLARE @new_id INT;
 *   EXEC [dbo].[sp_ims_copy_from_sysschedules]
 *       @sysschedule_id = 42,   -- ID from msdb.dbo.sysschedules
 *       @schedule_id    = @new_id OUTPUT;
 *   SELECT @new_id AS NewScheduleId;
 */
CREATE PROCEDURE [dbo].[sp_ims_copy_from_sysschedules]
(
    @sysschedule_id INT,
    @schedule_id INT OUTPUT
)
AS
BEGIN
    INSERT INTO [dbo].[ims_schedules]
    (
        [name],
        [freq_type],
        [freq_interval],
        [freq_subday_type],
        [freq_subday_interval],
        [freq_relative_interval],
        [freq_recurrence_factor],
        [active_start_date],
        [active_end_date],
        [active_start_time],
        [active_end_time]
    )
    
    SELECT
        [name],
        [freq_type],
        [freq_interval],
        [freq_subday_type],
        [freq_subday_interval],
        [freq_relative_interval],
        [freq_recurrence_factor],
        [active_start_date],
        [active_end_date],
        [active_start_time],
        [active_end_time]
    FROM [msdb].[dbo].[sysschedules]
    WHERE [schedule_id] = @sysschedule_id;
    
    SET @schedule_id = SCOPE_IDENTITY();
END;