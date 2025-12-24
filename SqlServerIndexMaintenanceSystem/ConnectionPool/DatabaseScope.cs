namespace SqlServerIndexMaintenanceSystem.ConnectionPool;

internal class DatabaseScope
{
    public int MaxThreads { get; set; }
    public SemaphoreSlim? Semaphore { get; set; }
}