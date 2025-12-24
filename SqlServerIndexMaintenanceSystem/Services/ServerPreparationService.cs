using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using SqlServerIndexMaintenanceSystem.ConnectionPool;
using SqlServerIndexMaintenanceSystem.Data;
using SqlServerIndexMaintenanceSystem.Models.Ims;

namespace SqlServerIndexMaintenanceSystem.Services;

public class ServerPreparationService
{
    private readonly SqlConnectionPool<IDbConnection> _clientConnectionPool;
    private readonly ILogger<ServerPreparationService> _logger;
    private ConcurrentBag<int> _knownServers = new();
    private ConcurrentBag<int> _knownDatabases = new();
    private ConcurrentBag<int> _cleanedDatabases = new();
    private ConcurrentDictionary<int, object> _serverLocks = new();
    private ConcurrentDictionary<int, object> _databaseLocks = new();

    public ServerPreparationService(
        SqlConnectionPool<IDbConnection> clientConnectionPool,
        ILogger<ServerPreparationService> logger
    )
    {
        _clientConnectionPool = clientConnectionPool;
        _logger = logger;
    }

    public bool PrepareMasterDatabaseIfNew(Server server, CancellationToken cancellationToken)
    {
        if (_knownServers.Contains(server.ServerId))
        {
            return true;
        }

        var lockObj = _serverLocks.GetOrAdd(server.ServerId, new object());

        lock (lockObj)
        {
            if (_knownServers.Contains(server.ServerId))
            {
                return true;
            }

            if (PrepareServerInternal(server, cancellationToken))
            {
                _knownServers.Add(server.ServerId);
                return true;
            }
        }

        return false;
    }

    public bool PrepareDatabaseIfNew(Server server, Database database, CancellationToken cancellationToken)
    {
        if (_knownDatabases.Contains(database.DatabaseId))
        {
            return true;
        }

        var lockObj = _databaseLocks.GetOrAdd(database.DatabaseId, new object());

        lock (lockObj)
        {
            if (_knownDatabases.Contains(database.DatabaseId))
            {
                return true;
            }

            if (PrepareDatabaseInternal(server, database, cancellationToken))
            {
                _knownDatabases.Add(database.DatabaseId);
                _cleanedDatabases = [.. _cleanedDatabases.Where(db => db != database.DatabaseId)];
                return true;
            }
        }

        return false;
    }

    public bool CleanDatabaseIfNew(Server server, Database database, CancellationToken cancellationToken)
    {
        if (_cleanedDatabases.Contains(database.DatabaseId))
        {
            return true;
        }

        var lockObj = _databaseLocks.GetOrAdd(database.DatabaseId, new object());

        lock (lockObj)
        {
            if (_cleanedDatabases.Contains(database.DatabaseId))
            {
                return true;
            }

            if (CleanDatabaseInternal(server, database, cancellationToken))
            {
                _cleanedDatabases.Add(database.DatabaseId);
                _knownDatabases = [.. _knownDatabases.Where(db => db != database.DatabaseId)];
                return true;
            }
        }

        return false;
    }

    private bool PrepareServerInternal(Server server, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Applying stored procedures to master database of {server}");
        try
        {
            using var connection = _clientConnectionPool.GetConnection(server.Name, "master", pooled: false, server.IntegratedSecurity, cancellationToken);
            connection.Open();

            connection.ApplyIndexDefragInfoStoredProcedure();

            connection.Close();

            return true;
        }
        catch (SqlException ex) when (ex.Number == 2714) // 'Object already exists' code
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to apply stored procedures to master database of {server}");
            return false;
        }
    }

    private bool CleanDatabaseInternal(Server server, Database database, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Dropping stored procedures on {database} of {server}");
        try
        {
            using var connection = _clientConnectionPool.GetConnection(server.Name, database.Name, pooled: false, server.IntegratedSecurity, cancellationToken);
            connection.Open();

            connection.DropIndexDefragInfoStoredProcedure();

            connection.Close();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to apply stored procedures to master database of {server}");
            return false;
        }
    }

    private bool PrepareDatabaseInternal(Server server, Database database, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Applying stored procedures to {database} of {server}");
        try
        {
            using var connection = _clientConnectionPool.GetConnection(server.Name, database.Name, pooled: false, server.IntegratedSecurity, cancellationToken);
            connection.Open();

            connection.ApplyIndexDefragInfoStoredProcedure();

            connection.Close();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to apply stored procedures to {database} of {server}");
            return false;
        }
    }
}