using System.Collections.Concurrent;
using System.Text;

namespace SqlServerIndexMaintenanceSystem.ConnectionPool;

internal class ParallelTasksManager
{
    private ConcurrentDictionary<string, ServerScope> _servers = new();

    /// <summary>
    /// Adds a server to the system with a maxThreads limit.
    /// Safe to use multiple times with the same serverId.
    /// </summary>
    public void AddServer(string serverId, int maxThreads)
    {
        var serverScope = new ServerScope
        {
            MaxThreads = maxThreads,
            Semaphore = maxThreads > 0 ? new SemaphoreSlim(maxThreads, maxThreads) : null,
        };

        _servers.AddOrUpdate(serverId, serverScope, (_, oldScope) =>
            oldScope.MaxThreads == maxThreads ? oldScope : serverScope
        );
    }

    /// <summary>
    /// Adds a database to the system with a maxThreads limit.
    /// Safe to use multiple times with the same (serverId, databaseId) pair.
    /// </summary>
    public void AddDatabase(string serverId, string databaseId, int maxThreads)
    {
        if (!_servers.TryGetValue(serverId, out var serverScope))
        {
            throw new InvalidOperationException($"Server with id {serverId} not found.");
        }

        var databaseScope = new DatabaseScope
        {
            MaxThreads = maxThreads,
            Semaphore = maxThreads > 0 ? new SemaphoreSlim(maxThreads, maxThreads) : null
        };

        serverScope.Databases.AddOrUpdate(databaseId, databaseScope, (_, oldScope) =>
            oldScope.MaxThreads == maxThreads ? oldScope : databaseScope
        );
    }

    public int? GetMaxThreads(string serverId, string? databaseId)
    {
        if (!_servers.TryGetValue(serverId, out var serverScope))
        {
            return null;
        }
        if (databaseId == null || !serverScope.Databases.TryGetValue(databaseId, out var db))
        {
            return serverScope.MaxThreads;
        }
        return db.MaxThreads;
    }

    public async Task<SyncObject> WaitAsync(string serverId, string databaseId, CancellationToken cancellationToken)
    {
        if (!_servers.TryGetValue(serverId, out var serverScope))
        {
            throw new InvalidOperationException($"Server with id {serverId} not found.");
        }
        if (!serverScope.Databases.TryGetValue(databaseId, out var _))
        {
            throw new InvalidOperationException($"Database with id {databaseId} not found.");
        }

        var server = _servers[serverId];
        var db = server.Databases[databaseId];

        if (db.Semaphore != null)
        {
            await db.Semaphore.WaitAsync(cancellationToken);
        }
        if (server.Semaphore != null)
        {
            await server.Semaphore.WaitAsync(cancellationToken);
        }

        return new SyncObject(server.Semaphore, db.Semaphore);
    }

    public SyncObject Wait(string serverId, string databaseId, CancellationToken cancellationToken)
    {
        if (!_servers.TryGetValue(serverId, out var serverScope))
        {
            throw new InvalidOperationException($"Server with id {serverId} not found.");
        }
        if (!serverScope.Databases.TryGetValue(databaseId, out var _))
        {
            throw new InvalidOperationException($"Database with id {databaseId} not found.");
        }

        var server = _servers[serverId];
        var db = server.Databases[databaseId];

        db.Semaphore?.Wait(cancellationToken);
        server.Semaphore?.Wait(cancellationToken);

        return new SyncObject(server.Semaphore, db.Semaphore);
    }

    public void Release(SyncObject? syncObj)
    {
        if (syncObj == null)
        {
            return;
        }

        if (syncObj.ServerSemaphore != null)
        {
            syncObj.ServerSemaphore?.Release();
        }
        if (syncObj.DatabaseSemaphore != null)
        {
            syncObj.DatabaseSemaphore?.Release();
        }
    }

    public void Log(ILogger logger)
    {
        var message = _servers.Aggregate(new StringBuilder("Available threads"), (sb, server) =>
        {
            sb.Append($": {server.Key}: {server.Value.Semaphore?.CurrentCount.ToString() ?? "unlimited"} [ ");

            foreach (var db in server.Value.Databases)
            {
                sb.Append($"db {db.Key}: {db.Value.Semaphore?.CurrentCount.ToString() ?? "unlimited"} ");
            }

            sb.Append("]");

            return sb;
        });

        logger.LogTrace(message.ToString());
    }
}
