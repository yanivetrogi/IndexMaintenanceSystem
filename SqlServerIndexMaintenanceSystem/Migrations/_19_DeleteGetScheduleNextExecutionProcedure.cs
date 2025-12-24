namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _19_DeleteGetScheduleNextExecutionProcedure
{
    public static string DeleteGetScheduleNextExecutionProcedureSql =
@"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ims_get_schedule_next_execution_date_and_time]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ims_get_schedule_next_execution_date_and_time]
";
}