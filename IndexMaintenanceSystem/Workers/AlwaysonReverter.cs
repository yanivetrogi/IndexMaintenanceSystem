using System.Data;
using Dapper;
using IndexMaintenanceSystem.ConnectionPool;
using IndexMaintenanceSystem.Data;
using IndexMaintenanceSystem.Models.Ims;
using IndexMaintenanceSystem.Services;

namespace IndexMaintenanceSystem.Workers;

public class AlwaysonReverter : BackgroundService
{
    private readonly ILogger<AlwaysonReverter> _logger;
    private readonly SynchronizationService _synchronizationService;
    private readonly SqlConnectionPool<IDbConnection> _clientConnectionPool;
    private readonly ImsConnectionFactory _imsDbConectionFactory;

    public AlwaysonReverter(
        ILogger<AlwaysonReverter> logger,
        SynchronizationService synchronizationService,
        ImsConnectionFactory imsDbConectionFactory,
        SqlConnectionPool<IDbConnection> clientConnectionPool)
    {
        _logger = logger;
        _synchronizationService = synchronizationService;
        _imsDbConectionFactory = imsDbConectionFactory;
        _clientConnectionPool = clientConnectionPool;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await _synchronizationService.WaitUntilMigrationFinished())
        {
            _logger.LogError("Terminating Alwayson Reverter due to migration failure");
            return;
        }

        await PerformAlwaysonRevertAsync(stoppingToken);

        _synchronizationService.MarkAlwaysonRevertAsFinished();
    }

    private async Task PerformAlwaysonRevertAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var imsConnection = _imsDbConectionFactory();
            imsConnection.Open();
            IEnumerable<DatabaseAlwayson> databases = await imsConnection.GetDatabasesWithAlwaysonAsync();

            foreach (var database in databases)
            {
                _logger.LogInformation($"Reverting Alwayson for database: {database.Database} on server: {database.Server}, AG: {database.AgName}");

                using var clientConnection = _clientConnectionPool.GetMasterConnection(database.Server, database.IntegratedSecurity);

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Operation cancelled while trying to revert Alwayson");
                    return;
                }

                if (clientConnection == null)
                {
                    _logger.LogError($"Failed to get connection for server: {database.Server}, database: master");
                    continue;
                }

                clientConnection.Open();

                var agSqls = await clientConnection.GetAgSqlsRevertForAgAsync(database.Database, database.AgName);

                foreach (var sql in agSqls)
                {
                    _logger.LogInformation($"Executing SQL for Alwayson revert: {sql}");
                    await clientConnection.ExecuteAsync(sql, cancellationToken);
                }

                clientConnection.Close();

                await imsConnection.RemoveAlwaysonDatabaseAsync(database.Server, database.Database);
            }

            imsConnection.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while performing Alwayson revert");
        }
        finally
        {
            _synchronizationService.MarkAlwaysonRevertAsFinished();
        }
    }

}
