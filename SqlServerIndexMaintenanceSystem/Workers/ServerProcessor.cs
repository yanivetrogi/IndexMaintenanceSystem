using System.Collections.Concurrent;
using System.Data;
using SqlServerIndexMaintenanceSystem.ConnectionPool;
using SqlServerIndexMaintenanceSystem.Data;
using SqlServerIndexMaintenanceSystem.Models.Client;
using SqlServerIndexMaintenanceSystem.Models.Ims;
using SqlServerIndexMaintenanceSystem.Services;

namespace SqlServerIndexMaintenanceSystem.Workers;

public class ServerProcessor : BackgroundService
{
    private readonly ILogger<ServerProcessor> _logger;
    private readonly ImsConnectionFactory _imsConnectionFactory;
    private readonly SqlConnectionPool<IDbConnection> _clientConnectionPool;
    private readonly SynchronizationService _syncService;
    private readonly int _executionIntervalSeconds;
    private readonly ConcurrentDictionary<string, Task> _ongoingExecutions = new();
    
    public ServerProcessor(
        ILogger<ServerProcessor> logger,
        IConfiguration configuration,
        ImsConnectionFactory imsConnectionFactory,
        SqlConnectionPool<IDbConnection> clientConnectionPool,
        SynchronizationService syncService)
    {
        _logger = logger;
        _imsConnectionFactory = imsConnectionFactory;
        _clientConnectionPool = clientConnectionPool;
        _syncService = syncService;
        _executionIntervalSeconds = configuration.GetValue("ExecutionIntervalSeconds", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!await _syncService.WaitUntilMigrationFinished())
        {
            _logger.LogError("Terminating defragger due to migration failure");
            return;
        }

        await _syncService.WaitUntilInitialReschedulingFinished();

        while (!cancellationToken.IsCancellationRequested)
        {
            Task[] tasks = [
                ProcessorIterationAsync(cancellationToken),
                Task.Delay(_executionIntervalSeconds * 1000, cancellationToken)
            ];

            Task.WaitAll(tasks, cancellationToken);
        }
    }

    private async Task ProcessorIterationAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Processing servers");

        try
        {
            using var dbConnection = _imsConnectionFactory();
            var imsDatabaseName = dbConnection.Database;
            dbConnection.Open();
            var executionsWithDetails = await dbConnection.GetServersToProcessAsync(_executionIntervalSeconds);
            dbConnection.Close();

            var intervalCancellationTokenSource = new CancellationTokenSource(_executionIntervalSeconds * 1000);

            foreach (var (server, nextCheck, schedule) in executionsWithDetails)
            {
                _clientConnectionPool.AddServer(server.Name, server.MaxThreads);

                if (!_ongoingExecutions.TryAdd(server.Name, Task.CompletedTask))
                    continue;

                _ongoingExecutions[server.Name] = ProcessServerAsync(server, imsDatabaseName, cancellationToken)
                    .ContinueWith(async task =>
                    {
                        await task;
                        _ongoingExecutions.TryRemove(server.Name, out var _);
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing servers");
        }
    }

    private async Task ProcessServerAsync(Server server, string imsDatabaseName, CancellationToken cancellationToken)
    {
        var processId = Guid.NewGuid();

        try
        {
            IList<DiscoveredDatabase> databases = new List<DiscoveredDatabase>();

            if (server.DiscoverDatabases) {
                using (var clientMasterConnection = _clientConnectionPool.GetMasterConnection(server.Name, server.IntegratedSecurity))
                {
                    if (clientMasterConnection == null || cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug($"[{processId}] Failed to acquire a connection to {server}. Terminating server processing.");
                        return;
                    }

                    clientMasterConnection.Open();

                    _logger.LogDebug($"[{processId}] Discovering databases for {server}");

                    databases = (await clientMasterConnection.DiscoverDatabasesAsync())
                        .Where(db => db.Name != imsDatabaseName)
                        .ToList();

                    clientMasterConnection.Close();
                }

                if (databases == null)
                {
                    _logger.LogDebug($"[{processId}] Failed to discover databases for {server}");
                }
                else if (databases.Count() > 0)
                {
                    foreach (var database in databases)
                    {
                        database.ServerId = server.ServerId;
                    }

                    _logger.LogInformation($"[{processId}] Databases discovered on {server}:\n\t{string.Join("\n\t", databases.Select(i => i.Name))}");
                }
                else
                {
                    _logger.LogInformation($"[{processId}] No database was found during discovery on {server}");
                }
            }

            using (var dbConnection = _imsConnectionFactory())
            {
                dbConnection.Open();

                if (server.DiscoverDatabases && databases != null) {
                    if (databases.Count() > 0)
                        await dbConnection.InsertDatabasesAsync(databases);

                    await dbConnection.TurnOffDatabaseDiscoveryAsync(server);
                }

                if (server.RunImmediately) {
                    await dbConnection.TurnOffRunImmediatelyAsync(server);
                    await dbConnection.TurnOnRunImmediatelyOfChildDatabasesAsync(server);
                }
                dbConnection.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while discovering databases for {server}");
        }
    }
}
