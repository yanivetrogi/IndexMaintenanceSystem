namespace IndexMaintenanceSystem.Migrations;

public static class _22_UpdateScheduleDescriptionToNullWhereError
{
    // This is done in order to trigger the rebuild of the descriptions
    public static string DeleteGetScheduleDescriptionStoredProcedureSql =
@"UPDATE ims_schedules
SET [description] = NULL
WHERE [description] LIKE 'Error%'
";
}
