using System.Data;

namespace SqlServerIndexMaintenanceSystem.Data;

public static class ImsConnectionStoredProceduresExtensions
{
    private static readonly string GetScheduleNextExecutionPath = "Scripts/sp_ims_get_schedule_next_execution_date_and_time.sql";
    private static readonly string PlanNextCheckPath = "Scripts/sp_ims_plan_next_check.sql";
    private static readonly string GetScheduleDescriptionPath = "Scripts/sp_ims_get_schedule_description.sql";
    private static readonly string AgentDatetimePath = "Scripts/f_agent_datetime.sql";
    private static readonly string NthRelativeDateOfMonthFunctionPath = "Scripts/f_ims_nth_relative_date_of_month.sql";
    private static readonly string MatchDayFunctionPath = "Scripts/f_ims_match_day.sql";
    private static readonly string NextTimeForDatePath = "Scripts/f_ims_next_time_for_date.sql";

    public static void ValidateStoredProcedureDefinitionFilesExist()
    {
        if (!SqlFromFileUtils.QueryExists(GetScheduleNextExecutionPath))
        {
            throw new Exception($"{GetScheduleNextExecutionPath} stored procedure definition file does not exist");
        }
        if (!SqlFromFileUtils.QueryExists(PlanNextCheckPath))
        {
            throw new Exception($"{PlanNextCheckPath} stored procedure definition file does not exist");
        }
        if (!SqlFromFileUtils.QueryExists(GetScheduleDescriptionPath))
        {
            throw new Exception($"{GetScheduleDescriptionPath} stored procedure definition file does not exist");
        }
        if (!SqlFromFileUtils.QueryExists(AgentDatetimePath))
        {
            throw new Exception($"{AgentDatetimePath} stored procedure definition file does not exist");
        }
    }

    public static async Task ApplyGetScheduleNextExecutionStoredProcedureAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(GetScheduleNextExecutionPath);

    public static async Task ApplyPlanNextCheckStoredProceduresAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(PlanNextCheckPath);

    public static async Task ApplyGetScheduleDescriptionStoredProceduresAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(GetScheduleDescriptionPath);

    public static async Task ApplyAgentDatetimeFunctionAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(AgentDatetimePath);
    
    public static async Task ApplyNthRelativeDateOfMonthFunctionAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(NthRelativeDateOfMonthFunctionPath);
    public static async Task ApplyMatchDayFunctionAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(MatchDayFunctionPath);
    public static async Task ApplyNextTimeForDateFunctionAsync(this IDbConnection imsConnection) =>
        await imsConnection.ApplyStoredProcedureAsync(NextTimeForDatePath);
}