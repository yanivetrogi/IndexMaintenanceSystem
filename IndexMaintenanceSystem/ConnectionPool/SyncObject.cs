namespace IndexMaintenanceSystem.ConnectionPool;

internal class SyncObject(SemaphoreSlim? serverSemaphore, SemaphoreSlim? databaseSemaphore)
{
    public SemaphoreSlim? ServerSemaphore { get; } = serverSemaphore;
    public SemaphoreSlim? DatabaseSemaphore { get; } = databaseSemaphore;
}
