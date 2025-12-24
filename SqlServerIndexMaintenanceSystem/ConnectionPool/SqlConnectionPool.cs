using System.Collections.Concurrent;
using System.Data;

namespace SqlServerIndexMaintenanceSystem.ConnectionPool;

// if the poolSize is null, then Pooling is managed by the underlying infrastructure  
// if the poolSize is 0, then Pooling is disabled (Pooling=false)
// if the poolSize is > 0, then Pooling is enabled with Max Pool Size set
public delegate T ConnectionFactory<T>(string server, string database, int? poolSize, bool integratedSecurity) where T : IDbConnection, IDisposable;

public class SqlConnectionPool<T>(
    ConnectionFactory<T> connectionFactory
) where T : IDbConnection, IDisposable
{
    private readonly ParallelTasksManager _parallelTasksManager = new();
    private readonly ConcurrentDictionary<PooledConnection<T>, (string Server, string Database)> _connections = new();

    public void AddServer(string serverId, int maxThreads)
    {
        _parallelTasksManager.AddServer(serverId, maxThreads);
    }

    public void AddDatabase(string serverId, string databaseId, int maxThreads)
    {
        _parallelTasksManager.AddDatabase(serverId, databaseId, maxThreads);
    }

    public async Task<PooledConnection<T>?> GetConnectionAsync(string server, string database, bool pooled, bool integratedSecurity, CancellationToken cancellationToken)
    {
        try
        {
            SyncObject? syncObject = null;

            if (database != "master" && pooled)
                syncObject = await _parallelTasksManager.WaitAsync(server, database, cancellationToken);

            // there might be no semaphore to wait on
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return GetConnectionInternal(server, database, pooled, integratedSecurity, syncObject);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public PooledConnection<T> GetConnection(string server, string database, bool pooled, bool integratedSecurity, CancellationToken cancellationToken)
    {
        SyncObject? syncObject = null;
        if (database != "master" && pooled)
            syncObject = _parallelTasksManager.Wait(server, database, cancellationToken);
        return GetConnectionInternal(server, database, pooled, integratedSecurity, syncObject);
    }

    public PooledConnection<T> GetMasterConnection(string server, bool integratedSecurity)
    {
        return GetConnectionInternal(server, "master", false, integratedSecurity, null);
    }

    private PooledConnection<T> GetConnectionInternal(string server, string database, bool pooled, bool integratedSecurity, SyncObject? syncObject)
    {
        var maxThreads = _parallelTasksManager.GetMaxThreads(server, database);

        int? poolSize = (database == "master" || !pooled) ? 0 : maxThreads;

        var connection = new PooledConnection<T>(connectionFactory(server, database, poolSize, integratedSecurity), this, syncObject);
        _connections.TryAdd(connection, (server, database));
        return connection;
    }

    public async void StartLoggingAsync(ILogger logger, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                _parallelTasksManager.Log(logger);
            }
        }
    }

    internal void ReleaseConnection(PooledConnection<T> pooledConnection, SyncObject? syncObject)
    {
        if (_connections.TryRemove(pooledConnection, out var address) && address.Database != "master")
        {
            _parallelTasksManager.Release(syncObject);
        }
    }
}