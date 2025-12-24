
namespace SqlServerIndexMaintenanceSystem.Services;

public class SynchronizationService
{
    private bool _migrationSuccess = false;
    private int _migrationSyncLock = 1;
    private int _initialRescheduleSyncLock = 1;
    private int _alwaysonRevertSyncLock = 1;

    public async Task<bool> WaitUntilMigrationFinished()
    {
        while (Interlocked.CompareExchange(ref _migrationSyncLock, 0, 0) == 1)
        {
            await Task.Delay(100);
        }

        return _migrationSuccess;
    }

    public void MarkMigrationAsFinished(bool success)
    {
        _migrationSuccess = success;
        Interlocked.Exchange(ref _migrationSyncLock, 0);
    }

    public async Task WaitUntilInitialReschedulingFinished()
    {
        while (Interlocked.CompareExchange(ref _initialRescheduleSyncLock, 0, 0) == 1)
        {
            await Task.Delay(100);
        }
    }

    public void MarkInitialReschedulingAsFinished()
    {
        Interlocked.Exchange(ref _initialRescheduleSyncLock, 0);
    }

    public async Task WaitUntilAlwaysonRevertFinished()
    {
        while (Interlocked.CompareExchange(ref _alwaysonRevertSyncLock, 0, 0) == 1)
        {
            await Task.Delay(100);
        }
    }

    public void MarkAlwaysonRevertAsFinished()
    {
        Interlocked.Exchange(ref _alwaysonRevertSyncLock, 0);
    }
}