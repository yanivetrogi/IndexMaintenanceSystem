namespace IndexMaintenanceSystem.Migrations;

public static class _21_DeleteGetScheduleDescriptionStoredProcedure
{
    public static string DeleteGetScheduleDescriptionStoredProcedureSql =
@"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ims_get_schedule_description]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_ims_get_schedule_description]
";
}
