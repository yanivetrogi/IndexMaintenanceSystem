using System.Collections.Concurrent;

namespace IndexMaintenanceSystem.ConnectionPool;

internal class ServerScope
{
    public int? MaxThreads { get; set; }
    public SemaphoreSlim? Semaphore { get; set; }
    public ConcurrentDictionary<string, DatabaseScope> Databases { get; set; } = new();
}

