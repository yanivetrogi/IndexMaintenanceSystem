using System.Collections.Concurrent;

namespace SqlServerIndexMaintenanceSystem.ConnectionPool;

internal class ServerScope
{
    public int? MaxThreads { get; set; }
    public SemaphoreSlim? Semaphore { get; set; }
    public ConcurrentDictionary<string, DatabaseScope> Databases { get; set; } = new();
}
