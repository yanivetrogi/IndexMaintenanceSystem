using System.Data;

namespace IndexMaintenanceSystem.ConnectionPool;

public class PooledConnection<T> : IDbConnection, IDisposable where T : IDbConnection
{
    private bool _disposed;

    private T UnderlyingConnection { get; set; }
    private SqlConnectionPool<T>? Pool { get; set; }
    private SyncObject? SyncObject { get; }

    internal PooledConnection(T connection, SqlConnectionPool<T> pool, SyncObject? syncObject)
    {
        UnderlyingConnection = connection;
        Pool = pool;
        SyncObject = syncObject;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Pool?.ReleaseConnection(this, SyncObject);
                UnderlyingConnection?.Dispose();
            }

            Pool = null;

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public string ConnectionString
    {
        get => UnderlyingConnection.ConnectionString;
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        set => UnderlyingConnection.ConnectionString = value;
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    }

    public int ConnectionTimeout => UnderlyingConnection.ConnectionTimeout;

    public string Database => UnderlyingConnection.Database;

    public ConnectionState State => UnderlyingConnection.State;

    public IDbTransaction BeginTransaction()
    {
        return UnderlyingConnection.BeginTransaction();
    }

    public IDbTransaction BeginTransaction(IsolationLevel il)
    {
        return UnderlyingConnection.BeginTransaction(il);
    }

    public void ChangeDatabase(string databaseName)
    {
        UnderlyingConnection.ChangeDatabase(databaseName);
    }

    public void Open()
    {
        UnderlyingConnection.Open();
    }

    public void Close()
    {
        UnderlyingConnection.Close();
    }

    public IDbCommand CreateCommand()
    {
        return UnderlyingConnection.CreateCommand();
    }

}

