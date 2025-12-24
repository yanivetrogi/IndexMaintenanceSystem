using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using SqlServerIndexMaintenanceSystem.Services;
using SqlServerIndexMaintenanceSystem.Migrations;
using SqlServerIndexMaintenanceSystem.Data;

namespace SqlServerIndexMaintenanceSystem.Workers;

public class ImsMigrator : BackgroundService
{
    public static readonly int TableCount = 7;
    public static readonly int TriggerCount = 5;
    public static readonly int StoredProcedureCount = 3;
    public static readonly int FunctionsCount = 3;
    public static readonly int HelperProcedureCount = 1;

    private static readonly string TablesExistSql =
@$"SELECT CAST(CASE WHEN COUNT(*) = {TableCount} THEN 1 ELSE 0 END as BIT)
FROM sys.tables t
WHERE t.[name] like 'ims_%'";
    private static readonly string TriggersExistSql =
@$"SELECT CAST(CASE WHEN COUNT(*) = {TriggerCount} THEN 1 ELSE 0 END AS BIT)
FROM sys.objects o
WHERE o.[name] like 'trigger_ims_%' AND o.[type]='TR'";
    private static readonly string SPsExistSql =
@$"SELECT CAST(CASE WHEN COUNT(*) BETWEEN {StoredProcedureCount} AND {StoredProcedureCount + HelperProcedureCount} THEN 1 ELSE 0 END AS BIT)
FROM sys.procedures p
WHERE p.[name] like 'sp_ims_%'";
    private static readonly string FNsExistSql =
@$"SELECT CAST(CASE WHEN COUNT(*) = {FunctionsCount} THEN 1 ELSE 0 END AS BIT)
FROM sys.objects
WHERE [type] IN ('FN', 'IF', 'TF')
and [name] like 'f_ims_%'";

    private readonly ILogger<ImsMigrator> _logger;
    private readonly ImsConnectionFactory _imsDbConectionFactory;
    private readonly SynchronizationService _syncService;

    public ImsMigrator(
        ILogger<ImsMigrator> logger,
        ImsConnectionFactory imsDbConectionFactory,
        SynchronizationService syncService)
    {
        _logger = logger;
        _imsDbConectionFactory = imsDbConectionFactory;
        _syncService = syncService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            ImsConnectionStoredProceduresExtensions.ValidateStoredProcedureDefinitionFilesExist();

            _logger.LogInformation("Checking the connection to the database...");

            if (!CanConnectToServer())
            {
                throw new Exception("Cannot connect to the server's master database. Please check the connection string.");
            }

            if (!CanConnectToDatabase())
            {
                _logger.LogInformation("Creating the database...");
                await CreateDbAsync();
                SqlConnection.ClearAllPools();

                _logger.LogInformation("Applying the schema...");
                await CreateSchemaSafeAsync();
                SqlConnection.ClearAllPools();
            } else {
                await MigrateSchemaSafeAsync();                
            }

            _logger.LogInformation("Validating the schema...");
            var validationError = await ValidateSchemaAsync();
            if (validationError != null)
            {
                throw new Exception($"Database schema is invalid. {validationError}");
            }

            SqlConnection.ClearAllPools();

            _logger.LogInformation("Database checked successfully");
            _syncService.MarkMigrationAsFinished(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            _syncService.MarkMigrationAsFinished(false);
            throw;
        }
    }

    private bool CanConnectToDatabase()
    {
        using var connection = _imsDbConectionFactory();
        try
        {
            connection.Open();
            connection.Close();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private bool CanConnectToServer()
    {
        using var connection = _imsDbConectionFactory();

        var connectionString = connection.ConnectionString;
        Regex regex = new Regex(@"Database=(.*?)(;|$)");
        var match = regex.Match(connectionString);
        var destDb = match.Groups[1].Value;
        var masterConnectionString = regex.Replace(connectionString, "Database=master;");

        using var masterConnection = new SqlConnection(masterConnectionString);
        try
        {
            masterConnection.Open();
            masterConnection.Close();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private async Task CreateDbAsync()
    {
        using var connection = _imsDbConectionFactory();

        var connectionString = connection.ConnectionString;
        Regex regex = new Regex(@"Database=(.*?)(;|$)");
        var match = regex.Match(connectionString);
        var destDb = match.Groups[1].Value;
        var masterConnectionString = regex.Replace(connectionString, "Database=master;");

        using var masterConnection = new SqlConnection(masterConnectionString);

        masterConnection.Open();

        try
        {
            await masterConnection.ExecuteAsync($"CREATE DATABASE {destDb}");

            var dbReadySql = $"SELECT CAST(CASE WHEN DATABASEPROPERTYEX('{destDb}', 'Collation') IS NOT NULL THEN 1 ELSE 0 END AS BIT)";

            while (!await masterConnection.QueryFirstAsync<bool>(dbReadySql))
            {
                await Task.Delay(500);
            }
        }
        finally
        {
            masterConnection.Close();
        }
    }

    private async Task CreateSchemaSafeAsync()
    {
        var ignoreIfExceptionNumber = new Func<int, Func<Task, Task>>((number) =>
            async (command) =>
            {
                try
                {
                    await command;
                }
                catch (SqlException ex) when (ex.Number == number)
                {
                    // Ignore if exception is expected
                }
            }
        );

        var ignoreIfExists = ignoreIfExceptionNumber(2714); // 'Object already exists' code

        var ignoreIfDoesNotExist = ignoreIfExceptionNumber(3701); // 'Table does not exist' code

        using var connection = _imsDbConectionFactory();

        connection.Open();

        try
        {
            await ignoreIfExists(connection.ExecuteAsync(_00_Schema.CreateTablesSql));
            await ignoreIfExists(connection.ApplyGetScheduleDescriptionStoredProceduresAsync());
            await ignoreIfExists(connection.ExecuteAsync(_01_SchedulesDescriptionTrigger.CreateSchedulesDescriptionTriggerSql));
            await ignoreIfExists(connection.ApplyAgentDatetimeFunctionAsync());
            await ignoreIfExists(connection.ApplyNthRelativeDateOfMonthFunctionAsync());
            await ignoreIfExists(connection.ApplyMatchDayFunctionAsync());
            await ignoreIfExists(connection.ApplyNextTimeForDateFunctionAsync());
            await ignoreIfExists(connection.ApplyGetScheduleNextExecutionStoredProcedureAsync());
            await ignoreIfExists(connection.ApplyPlanNextCheckStoredProceduresAsync());
            await ignoreIfExists(connection.ExecuteAsync(_02_ServersTrigger.CreateServersTriggerSql));
            await ignoreIfExists(connection.ExecuteAsync(_03_DatabasesTrigger.CreateDatabasesTriggerSql));
            await ignoreIfExists(connection.ExecuteAsync(_04_IndexesTrigger.CreateIndexesTriggerSql));
            await ignoreIfExists(connection.ExecuteAsync(_11_SchedulesReplanTrigger.CreateSchedulesReplanTriggerSql));
        }
        finally
        {
            connection.Close();
        }
    }

    private async Task MigrateSchemaSafeAsync()
    {
        var ignoreIfExceptionNumber = new Func<int, Func<Task, Task>>((number) =>
            async (command) =>
            {
                try
                {
                    await command;
                }
                catch (SqlException ex) when (ex.Number == number)
                {
                    // Ignore if exception is expected
                }
            }
        );

        var ignoreIfExists = ignoreIfExceptionNumber(2714); // 'Object already exists' code

        var ignoreIfDoesNotExist = ignoreIfExceptionNumber(3701); // 'Table does not exist' code

        using var connection = _imsDbConectionFactory();

        connection.Open();

        try
        {


            await connection.ExecuteAsync(_10_MaxDopToTinyint.AlterMaxdopFromBitToTinyintSql);
            await ignoreIfExists(connection.ExecuteAsync(_11_SchedulesReplanTrigger.CreateSchedulesReplanTriggerSql));
            await connection.ExecuteAsync(_12_ExcludeLastPartitionToEveryLevel.ExcludeLastPartitionToEveryLevelSql);
            await connection.ExecuteAsync(_13_IndexMinSize.IndexMinSizeSql);
            await connection.ExecuteAsync(_14_TlogFactors.TlogFactorsColumnsSql);
            await connection.ExecuteAsync(_15_IndexNameNullable.IndexNameNullableSql);
            await connection.ExecuteAsync(_16_AlwaysonDatabasesTable.CreateAlwaysonDatabasesTableSql);
            await connection.ExecuteAsync(_17_TlogSettingsToServer.TlogSettingsToServerSql);
            await connection.ExecuteAsync(_18_EnableAlwaysOnSetting.EnableAlwaysOnSettingSql);
            await connection.ExecuteAsync(_19_DeleteGetScheduleNextExecutionProcedure.DeleteGetScheduleNextExecutionProcedureSql);
            
            await ignoreIfExists(connection.ApplyNthRelativeDateOfMonthFunctionAsync());
            await ignoreIfExists(connection.ApplyMatchDayFunctionAsync());
            await ignoreIfExists(connection.ApplyNextTimeForDateFunctionAsync());

            await connection.ApplyGetScheduleNextExecutionStoredProcedureAsync();

            await connection.ExecuteAsync(_20_AddIntegratedSecurityColumnToServer.AddIntegratedSecurityColumnToServerSql);
            await connection.ExecuteAsync(_21_DeleteGetScheduleDescriptionStoredProcedure.DeleteGetScheduleDescriptionStoredProcedureSql);
            await connection.ApplyGetScheduleDescriptionStoredProceduresAsync();
            await connection.ExecuteAsync(_22_UpdateScheduleDescriptionToNullWhereError.DeleteGetScheduleDescriptionStoredProcedureSql);
            
            // Ensure core triggers exist (for robustness against broken schemas)
            await ignoreIfExists(connection.ExecuteAsync(_01_SchedulesDescriptionTrigger.CreateSchedulesDescriptionTriggerSql));
            await ignoreIfExists(connection.ExecuteAsync(_02_ServersTrigger.CreateServersTriggerSql));
            await ignoreIfExists(connection.ExecuteAsync(_03_DatabasesTrigger.CreateDatabasesTriggerSql));
            await ignoreIfExists(connection.ExecuteAsync(_04_IndexesTrigger.CreateIndexesTriggerSql));
        }
        finally
        {
            connection.Close();
        }
    }

    private async Task<string?> ValidateSchemaAsync()
    {
        using var connection = _imsDbConectionFactory();

        connection.Open();

        try
        {
            var errors = new List<string>();

            var tablesExist = await connection.QueryFirstAsync<bool>(TablesExistSql);
            if (!tablesExist) errors.Add($"Tables count mismatch. Expected: {TableCount}");

            var triggersCount = await connection.QueryFirstAsync<int>(
                "SELECT COUNT(*) FROM sys.objects o WHERE o.[name] like 'trigger_ims_%' AND o.[type]='TR'");
            if (triggersCount != TriggerCount) errors.Add($"Triggers count mismatch. Expected: {TriggerCount}, Found: {triggersCount}");

            var spsExist = await connection.QueryFirstAsync<bool>(SPsExistSql);
            if (!spsExist) errors.Add($"SPs count mismatch. Expected between {StoredProcedureCount} and {StoredProcedureCount + HelperProcedureCount}");

            var fnsExist = await connection.QueryFirstAsync<bool>(FNsExistSql);
            if (!fnsExist) errors.Add($"Functions count mismatch. Expected: {FunctionsCount}");

            return errors.Any() ? string.Join(", ", errors) : null;
        }
        finally
        {
            connection.Close();
        }
    }
}
