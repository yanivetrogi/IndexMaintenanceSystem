using System.Collections.Concurrent;
using Dapper;
using IndexMaintenanceSystem.Data;
using IndexMaintenanceSystem.Services;
using IndexMaintenanceSystem.Models.Ims;
using Index = IndexMaintenanceSystem.Models.Ims.Index;
using IndexMaintenanceSystem.ConnectionPool;
using System.Data;
using IndexMaintenanceSystem.Models.Client;
using Microsoft.Extensions.Options;

namespace IndexMaintenanceSystem.Workers;

public class DatabaseDefragger : BackgroundService
{
    private readonly ILogger<DatabaseDefragger> _logger;
    private readonly ImsConnectionFactory _imsDbConectionFactory;
    private readonly SqlConnectionPool<IDbConnection> _clientConnectionPool;
    private readonly SynchronizationService _syncService;
    private readonly ServerPreparationService _serverPreparationService;
    private readonly IOptions<GlobalConfig> _globalConfigOptions;
    private readonly ConcurrentDictionary<(string Server, string Database, string Index), Task> _ongoingIndexExecutions = new();
    private readonly ConcurrentDictionary<(string Server, string Database), Task> _ongoingDatabaseExecutions = new();

    private int _executionIntervalSeconds => _globalConfigOptions.Value.ExecutionIntervalSeconds ?? 30;

    public DatabaseDefragger(
        ILogger<DatabaseDefragger> logger,
        IOptions<GlobalConfig> globalConfigOptions,
        ImsConnectionFactory imsDbConnectionFactory,
        SqlConnectionPool<IDbConnection> clientConnectionPool,
        SynchronizationService syncService,
        ServerPreparationService serverPreparationService)
    {
        _logger = logger;
        _imsDbConectionFactory = imsDbConnectionFactory;
        _clientConnectionPool = clientConnectionPool;
        _syncService = syncService;
        _serverPreparationService = serverPreparationService;
        _globalConfigOptions = globalConfigOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        ClientConnectionStoredProceduresExtension.ValidateStoredProcedureDefinitionFilesExist();

        if (!await _syncService.WaitUntilMigrationFinished())
        {
            _logger.LogError("Terminating defragger due to migration failure");
            return;
        }

        await _syncService.WaitUntilInitialReschedulingFinished();

        await _syncService.WaitUntilAlwaysonRevertFinished();

        _clientConnectionPool.StartLoggingAsync(_logger, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Task[] tasks = [
                RunNextChecksAsync(cancellationToken),
                Task.Delay(_executionIntervalSeconds * 1000, cancellationToken)
            ];

            Task.WaitAll(tasks, cancellationToken);
        }

        _logger.LogTrace("DatabaseDefragger is stopping");
    }

    /// <remarks>
    /// This method is expected to complete quickly.
    /// It doesn't wait for the defraggings to finish.
    /// </remarks>
    private async Task RunNextChecksAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Running next checks");

        try
        {
            using var dbConnection = _imsDbConectionFactory();
            dbConnection.Open();
            var indexesToProcess = await dbConnection.GetNextIndexesToProcessAsync(_executionIntervalSeconds);
            var databasesToProcess = await dbConnection.GetNextDbsToProcessAsync(_executionIntervalSeconds);
            dbConnection.Close();

            foreach (var (server, database, index, nextCheck, schedule) in indexesToProcess)
            {
                var registered = RegisterServerAndDatabase(server, database, cancellationToken);

                if (!registered || !_ongoingIndexExecutions.TryAdd((server.Name, database.Name, index!.Name!), Task.CompletedTask))
                {
                    continue;
                }

                _ongoingIndexExecutions[(server.Name, database.Name, index.Name!)] =
                    CheckIndexAsync(server, database, index, schedule, nextCheck, cancellationToken)
                        .ContinueWith(async task =>
                        {
                            await task;
                            _ongoingIndexExecutions.Remove((server.Name, database.Name, index.Name!), out var _);
                        });
            }

            foreach (var (server, database, indexOverrides, nextCheck, schedule) in databasesToProcess)
            {
                var registered = RegisterServerAndDatabase(server, database, cancellationToken);

                if (!registered || !_ongoingDatabaseExecutions.TryAdd((server.Name, database.Name), Task.CompletedTask))
                {
                    continue;
                }

                _ongoingDatabaseExecutions[(server.Name, database.Name)] =
                    CheckDatabaseAsync(server, database, schedule, indexOverrides, nextCheck, cancellationToken)
                        .ContinueWith(async task =>
                        {
                            await task;
                            _ongoingDatabaseExecutions.Remove((server.Name, database.Name), out var _);
                        });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while running next checks");
        }
    }

    private bool RegisterServerAndDatabase(Server server, Database database, CancellationToken cancellationToken)
    {
        _clientConnectionPool.AddServer(server.Name, server.MaxThreads);
        _clientConnectionPool.AddDatabase(server.Name, database.Name, database.MaxThreads);

        if (!_serverPreparationService.PrepareMasterDatabaseIfNew(server, cancellationToken))
        {
            _logger.LogError($"Failed to prepare master database of {server}. Trying to prepare {database}.");

            if (!_serverPreparationService.PrepareDatabaseIfNew(server, database, cancellationToken))
            {
                _logger.LogError($"Failed to prepare {database} of {server}. Can't proceed.");
                return false;
            }
        }
        else
        {
            if (!_serverPreparationService.CleanDatabaseIfNew(server, database, cancellationToken))
            {
                _logger.LogWarning($"Failed to drop stored procedures on {database} of {server}.");
            }
        }

        return true;
    }

    private async Task CheckIndexAsync(Server server, Database database, Index index, Schedule? schedule, NextCheck? nextCheck, CancellationToken cancellationToken)
    {
        var processId = Guid.NewGuid();

        var reason = schedule == null ? "run_immediately flag" : $"schedule {schedule}";

        var indexRebuildThreshold = index.RebuildThreshold ?? database.RebuildThreshold ?? server.RebuildThreshold;
        var indexReorganizeThreshold = index.ReorganizeThreshold ?? database.ReorganizeThreshold ?? server.ReorganizeThreshold;
        var online = index.Online ?? database.Online ?? server.Online;
        var maxdop = index.Maxdop ?? database.Maxdop ?? server.Maxdop;
        var sortInTempdb = index.SortInTempdb ?? database.SortInTempdb ?? server.SortInTempdb;
        var indexMinSizeKb = index.IndexMinSizeKb ?? database.IndexMinSizeKb ?? server.IndexMinSizeKb;
        var excludeLastPartition = index.ExcludeLastPartition ?? database.ExcludeLastPartition ?? server.ExcludeLastPartition;
        var tlogGrowthFactor = index.TlogGrowthFactor ?? database.TlogGrowthFactor ?? server.TlogGrowthFactor;
        var tlogSizeFactor = index.TlogSizeFactor ?? database.TlogSizeFactor ?? server.TlogSizeFactor;

        var enableTlogDiskCheck = server.EnableTlogDiskCheck;
        var enableTlogFileCheck = server.EnableTlogFileCheck;
        var diskMinRemainingMb = server.DiskMinRemainingMb;
        var diskSafetyPct = server.DiskSafetyPct;

        var enableAlwaysOnCheck = database.EnableAlwaysOnCheck ?? server.EnableAlwaysOnCheck;

        var isAlwaysOnDatabase = false;

        var integratedSecurity = server.IntegratedSecurity;

        var historyEntry = new HistoryEntry(processId)
        {
            Server = server.Name,
            Database = database.Name,
            Schema = index.Schema,
            Table = index.Table,
            Index = index.Name!,
            Reason = reason,
            RebuildThreshold = indexRebuildThreshold,
            ReorganizeThreshold = indexReorganizeThreshold,
            Online = online,
            Maxdop = maxdop,
            SortInTempdb = sortInTempdb,
            IndexMinSizeKb = indexMinSizeKb,
            TlogGrowthFactor = tlogGrowthFactor,
            TlogSizeFactor = tlogSizeFactor,
            EnableTlogDiskCheck = enableTlogDiskCheck, 
            EnableTlogFileCheck = enableTlogFileCheck, 
            EnableAlwaysOnCheck = enableAlwaysOnCheck,
            DiskMinRemainingMb = diskMinRemainingMb, 
            DiskSafetyPct = diskSafetyPct, 
        };

        try
        {
            using var clientConnection = await _clientConnectionPool.GetConnectionAsync(server.Name, database.Name, pooled: true, server.IntegratedSecurity, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation($"[{processId}] The cancellation of {index} check was issued.");
                return;
            }

            if (clientConnection == null)
            {
                _logger.LogDebug($"[{processId}] Failed to acquire a connection to {server} and {database}. Terminating the check of {index}.");
                return;
            }

            HistoryEntrySkipped? skippedStatus = null;
            string? msg = null;

            // if index is inactive
            if (!index.Active && index.IndexId > 0)
            {
                _logger.LogWarning($"[{processId}] {index} on {database} of {server} is skipped, because it is inactive");
                skippedStatus = HistoryEntrySkipped.INACTIVE;
                msg = "Index is inactive";
            }
            // if this run is a scheduled one, while index has it's own schedule
            else if (nextCheck != null && index.ScheduleId != null && nextCheck.IndexId == null)
            {
                _logger.LogWarning($"[{processId}] {index} on {database} of {server} is skipped, because it is configured to be processed separately");
                skippedStatus = HistoryEntrySkipped.OWN_SCHEDULE;
                msg = "Index is configured to be processed separately";
            }
            // if this run is a run_immediately one, while index is explicitly set to not run immediately
            else if (nextCheck == null && index.RunImmediately == false)
            {
                _logger.LogWarning($"[{processId}] {index} on {database} of {server} is skipped, because it is explicitly set to not being run immediately");
                skippedStatus = HistoryEntrySkipped.RUN_IMMEDIATELY_DISABLED;
                msg = "Index is explicitly set to not being run immediately";
            }

            if (skippedStatus != null)
            {
                historyEntry.MarkAsSkipped(skippedStatus.Value, msg!);
                using (var dbConnection = _imsDbConectionFactory())
                {
                    dbConnection.Open();
                    await dbConnection.InsertHistoryEntryAsync(historyEntry);
                    dbConnection.Close();
                }
                return;
            }

            if (enableAlwaysOnCheck == true)
            {
                isAlwaysOnDatabase = await ChangeAvailabilityModeAsync(processId, server.Name, server.IntegratedSecurity, database.Name, false, cancellationToken);
            }

            clientConnection.Open();

            _logger.LogInformation($"[{processId}] Processing {index} on {database} of {server}:\n\treason: {reason}\n\trebuild threshold - {indexRebuildThreshold?.ToString() ?? "no"}\n\treorganize threshold - {indexReorganizeThreshold?.ToString() ?? "no"}");

            var indexSysInfo = await clientConnection.GetIndexSysInfoAsync(index);
            if (indexSysInfo == null || indexSysInfo.PartitionCount == 0)
            {
                var shouldInactivate = index.IndexId != 0;

                _logger.LogError($"[{processId}] Failed to get sys info. Might be a typo.{(shouldInactivate ? $" Inactivating the {index}." : "")}");
                historyEntry.MarkAsFailed(null, $"Failed to get sys info. Might be a typo.{(shouldInactivate ? " Inactivating the index" : "")}");

                using (var dbConnection = _imsDbConectionFactory())
                {
                    dbConnection.Open();
                    await dbConnection.InsertHistoryEntryAsync(historyEntry);
                    if (shouldInactivate)
                    {
                        await dbConnection.InactivateIndexAsync(index);
                    }
                    dbConnection.Close();
                }
                return;
            }

            historyEntry.UpdateWithSysInfo(indexSysInfo);

            var hasMultiplePartitions = indexSysInfo.PartitionCount > 1;
            var partitionCount = hasMultiplePartitions && excludeLastPartition ? indexSysInfo.PartitionCount - 1 : indexSysInfo.PartitionCount;

            var lastExcludedMessage = excludeLastPartition ? " (last partition is excluded)" : "";

            if (hasMultiplePartitions)
                _logger.LogInformation($"[{processId}] {index} has {indexSysInfo.PartitionCount} partition(s){lastExcludedMessage}");

            var success = true;

            for (var partitionNumber = 1; partitionNumber <= partitionCount && success; partitionNumber++)
            {
                var partitionHistoryEntry = (HistoryEntry)historyEntry.Clone();
                var partitionNumberOf = hasMultiplePartitions ? $"partition {partitionNumber}/{indexSysInfo.PartitionCount} of " : "";
                var inserted = false;

                try
                {
                    var indexInfo = await clientConnection.GetIndexDefragInfoAsync(
                        index,
                        indexSysInfo,
                        partitionNumber,
                        indexRebuildThreshold,
                        indexReorganizeThreshold,
                        online,
                        maxdop,
                        sortInTempdb,
                        buildCommand: true);

                    if (indexInfo == null)
                        throw new Exception($"Failed to get defrag info");

                    var actionRequired = indexInfo.Action != null && indexInfo.Command != null;

                    partitionHistoryEntry.UpdateWithStartDefragInfo(
                        indexInfo,
                        startTime: actionRequired ? DateTime.Now : null,
                        partitionNumber: hasMultiplePartitions ? partitionNumber : null);

                    if (!actionRequired)
                    {
                        msg = "No action required";
                        _logger.LogInformation($"[{processId}] {partitionNumberOf}{index}: {msg}");
                        partitionHistoryEntry.MarkAsSkipped(HistoryEntrySkipped.NOT_NEEDED, msg);
                        continue;
                    }

                    // Check index size against indexMinSizeKb.HasValue()
                    else if (indexMinSizeKb.HasValue && indexInfo.SizeKB < indexMinSizeKb.Value)
                    {
                        msg = $"Size is less then {indexMinSizeKb.Value} Kb";
                        _logger.LogWarning($"[{processId}] {partitionNumberOf}{index}: {msg}");
                        partitionHistoryEntry.MarkAsSkipped(HistoryEntrySkipped.INDEX_MIN_SIZE, msg);
                        continue;
                    }

                    // Check index size against transaction log size if configured
                    if (enableTlogFileCheck == true && tlogSizeFactor != null && tlogSizeFactor > 0)
                    {
                        /*
                            Decide the tlog size threshold that upon crossing skip an index.
                            For example: tlog size 10GB, index size 8GB.
                            If the tlogSizeFactor = 1 then the index will be processed (8 GB * 1 = 8 GB so the index size is smaller than the tlog size).
                            If tlog_size_factor = 0.4 then 8GB * 1.4 = 11.2 which is larger than the tlog size - we skip the index.
                            If the tlog_size_factor = 2 then any idex will have to be half the size of the tlog
                        */

                        var transactionLogSizeMb = await clientConnection.GetTransactionLogSizeMbAsync();

                        if (indexInfo.SizeKB / 1024 * tlogSizeFactor > transactionLogSizeMb)
                        {
                            msg = $"Size is over configured threshold: {indexInfo.SizeKB / 1024} MB * {tlogSizeFactor} >= {transactionLogSizeMb} MB";
                            _logger.LogWarning($"[{processId}] {partitionNumberOf}{index}: {msg}");
                            partitionHistoryEntry.MarkAsSkipped(HistoryEntrySkipped.TLOG_SIZE, msg);
                            continue;
                        }
                    }

                    if (enableTlogDiskCheck == true && tlogGrowthFactor.HasValue)
                    {
                        var availableSpaceMb = await clientConnection.GetTransactionLogAvailableSizeMbAsync();

                        if (diskSafetyPct.HasValue)
                        {
                            if (indexInfo.SizeKB / 1024 * tlogGrowthFactor > availableSpaceMb * diskSafetyPct / 100)
                            {
                                msg = $"Potential growth of transaction log is over the safety percent: {indexInfo.SizeKB / 1024} MB * {tlogGrowthFactor:N0} > {availableSpaceMb} MB * {diskSafetyPct}%";
                                _logger.LogWarning($"[{processId}] {partitionNumberOf}{index}: {msg}");
                                partitionHistoryEntry.MarkAsSkipped(HistoryEntrySkipped.TLOG_DISK_SAFETY_PERCENT, msg);
                                continue;
                            }
                        }

                        if (diskMinRemainingMb.HasValue)
                        {
                            if (availableSpaceMb - (indexInfo.SizeKB / 1024 * tlogGrowthFactor) < diskMinRemainingMb)
                            {
                                msg = $"Not enough disk space: {availableSpaceMb} MB - ({indexInfo.SizeKB / 1024} * {tlogGrowthFactor:N0}) MB < {diskMinRemainingMb:N0} MB";
                                _logger.LogWarning($"[{processId}] {partitionNumberOf}{index}: {msg}");
                                partitionHistoryEntry.MarkAsSkipped(HistoryEntrySkipped.DISK_MIN_REMAINING_SPACE, msg);
                                continue;
                            }
                        }
                    }

                    using (var dbConnection = _imsDbConectionFactory())
                    {
                        dbConnection.Open();
                        await dbConnection.InsertHistoryEntryAsync(partitionHistoryEntry);
                        dbConnection.Close();
                    }
                    inserted = true;

                    var startTime = DateTime.Now;
                    var command = partitionHistoryEntry.Command;

                    #if DEBUG
                        command += "; WAITFOR DELAY '00:00:20'";
                    #endif

                    await clientConnection.ExecuteAsync(new CommandDefinition(command!, commandTimeout: 0));

                    var endTime = DateTime.Now;

                    var indexInfoAfter = await clientConnection.GetIndexDefragInfoAsync(
                        index,
                        indexSysInfo,
                        partitionNumber);

                    if (indexInfoAfter == null)
                        throw new Exception($"Failed to get defrag info after defragging");

                    partitionHistoryEntry.MarkAsCompleted(indexInfoAfter, startTime, endTime);

                    _logger.LogInformation($"[{processId}] Defragmentation of {partitionNumberOf}{index} completed within {endTime - partitionHistoryEntry.StartTime:hh\\:mm\\:ss}");

                    if (enableAlwaysOnCheck == true && isAlwaysOnDatabase)
                    {
                        await ChangeAvailabilityModeAsync(processId, server.Name, server.IntegratedSecurity, database.Name, true, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    success = false;

                    _logger.LogError(ex, $"[{processId}] An error occurred while defragging {partitionNumberOf}{index}");

                    partitionHistoryEntry.MarkAsFailed(DateTime.Now, $"An error occurred while defragging: {ex.Message}");
                }
                finally
                {
                    using (var dbConnection = _imsDbConectionFactory())
                    {
                        dbConnection.Open();
                        if (inserted)
                            await dbConnection.UpdateHistoryEntryWithEndValuesAsync(partitionHistoryEntry);
                        else
                            await dbConnection.InsertHistoryEntryAsync(partitionHistoryEntry);
                        dbConnection.Close();
                    }
                }
            }

            // Update next check only if this is an index-level check.
            // Db-level checks are updated after all indexes being processed - by CheckDatabaseAsync function.
            if (success)
            {
                if (nextCheck?.IndexId == index.IndexId)
                {
                    using (var dbConnection = _imsDbConectionFactory())
                    {
                        dbConnection.Open();
                        await dbConnection.RescheduleNextCheckIfExistsAsync(nextCheck);
                        dbConnection.Close();
                    }
                }
                else if (index.RunImmediately == true)
                {
                    using (var dbConnection = _imsDbConectionFactory())
                    {
                        dbConnection.Open();
                        await dbConnection.TurnOffRunImmediatelyAsync(index);
                        dbConnection.Close();
                    }
                }
            }
            else
            {
                _logger.LogError($"[{processId}] Terminating the check of {index} because of the error above. It'll be reprocessed in the next run.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{processId}] An error occurred while defragging {index} on {database} of {server}");
        }
    }

    private async Task CheckDatabaseAsync(Server server, Database database, Schedule? schedule, List<Index> indexOverrides, NextCheck? nextCheck, CancellationToken cancellationToken)
    {
        var processId = Guid.NewGuid();

        var searchIndexes = database.SearchIndexes ?? server.SearchIndexes;

        var enableAlwaysOnCheck = database.EnableAlwaysOnCheck ?? server.EnableAlwaysOnCheck;

        var isAlwaysOnDatabase = false;

        try
        {
            List<Index> indexesToDefrag = new();

            if (searchIndexes)
            {
                IEnumerable<DiscoveredIndex> searchedIndexes = new List<DiscoveredIndex>();

                using (var clientConnection = await _clientConnectionPool.GetConnectionAsync(server.Name, database.Name, pooled: false, server.IntegratedSecurity, cancellationToken))
                {
                    if (clientConnection == null || cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug($"[{processId}] Failed to acquire a connection to {server} and {database}. Terminating the check of {database}");
                        return;
                    }

                    clientConnection.Open();

                    _logger.LogDebug($"[{processId}] Discovering indexes on {database} of {server}");

                    searchedIndexes = await clientConnection.DiscoverIndexesAsync();

                    clientConnection.Close();
                }

                if (searchedIndexes == null)
                {
                    _logger.LogError($"[{processId}] Failed to discover indexes on {database} of {server}. Terminating the check of {database}");
                    return;
                }

                if (searchedIndexes.Count() > 0)
                {
                    foreach (var index in searchedIndexes)
                    {
                        index.DatabaseId = database.DatabaseId;
                    }

                    _logger.LogDebug($"[{processId}] Indexes found on {database} of {server}:\n\t{string.Join("\n\t", searchedIndexes.Select(i => i.Name))}");
                }
                else
                {
                    _logger.LogInformation($"[{processId}] No index was found during search on {database} of {server}");
                }

                foreach (var searchedIndex in searchedIndexes)
                {
                    var indexOverride = indexOverrides.FirstOrDefault(i => i.Schema == searchedIndex.Schema && i.Table == searchedIndex.Table && (i.Name == searchedIndex.Name || string.IsNullOrEmpty(i.Name)));

                    if (indexOverride == null)
                    {
                        indexesToDefrag.Add(new Index()
                        {
                            Schema = searchedIndex.Schema,
                            Table = searchedIndex.Table,
                            Name = searchedIndex.Name,
                            Active = true,
                        });

                        continue;
                    }

                    if (string.IsNullOrEmpty(indexOverride.Name))
                    {
                        indexOverride = indexOverride.DeepCopy();
                        indexOverride.Name = searchedIndex.Name;
                    }

                    indexesToDefrag.Add(indexOverride);
                }
            }
            else
            {
                indexesToDefrag = indexOverrides;
            }

            if (indexesToDefrag.Count == 0)
            {
                _logger.LogInformation($"[{processId}] Nothing to defrag on {database} of {server}");
            }
            else
            {
                if (enableAlwaysOnCheck == true)
                {
                    isAlwaysOnDatabase = await ChangeAvailabilityModeAsync(processId, server.Name, server.IntegratedSecurity, database.Name, false, cancellationToken);
                }
            }

            var defragCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var tasks = indexesToDefrag.Select(index =>
            {
                if (!_ongoingIndexExecutions.TryAdd((server.Name, database.Name, index!.Name!), Task.CompletedTask))
                {
                    return Task.CompletedTask;
                }

                var checkTask = CheckIndexAsync(server, database, index, schedule, nextCheck, defragCancellationTokenSource.Token)
                    .ContinueWith(async task =>
                    {
                        await task;
                        _ongoingIndexExecutions.Remove((server.Name, database.Name, index.Name!), out var _);
                    });

                _ongoingIndexExecutions[(server.Name, database.Name, index.Name!)] = checkTask;

                return checkTask;
            }).ToList();

            if (nextCheck != null)
            {
                // Current mechanism is meant to gracefully stop the process, when the schedule active window is closing:
                // In parallel with actual execution there is a recurrent check of associated next_check.
                // Whenever it is rescheduled far enough ( more then _executionIntervalSeconds into future ), the shutdown is initiated.
                // The in-progress defrags are going to be finished, while not started onces are going to be aborted.

                var nextCheckActivityCheckingTask = this.WaitForNextCheckToBecomeInactive(nextCheck, defragCancellationTokenSource.Token);

                var completionTask = Task.WhenAll(tasks);

                var firstCompleted = await Task.WhenAny([nextCheckActivityCheckingTask, completionTask]);

                if (firstCompleted == nextCheckActivityCheckingTask)
                {
                    _logger.LogInformation($"[{processId}] The active window of the schedule {schedule} is finished.\nInitiating a soft stop (the ongoing defragmentations will be finished, the non-started ones will be aborted).");
                    defragCancellationTokenSource.Cancel();
                }

                await completionTask;

                using (var dbConnection = _imsDbConectionFactory())
                {
                    dbConnection.Open();
                    await dbConnection.RescheduleNextCheckIfExistsAsync(nextCheck);

                    dbConnection.Close();
                }
            }
            else
            {
                Task.WaitAll(tasks.ToArray());

                if (database.RunImmediately == true)
                {
                    using (var dbConnection = _imsDbConectionFactory())
                    {
                        dbConnection.Open();
                        await dbConnection.TurnOffRunImmediatelyAsync(database);
                        dbConnection.Close();
                    }
                }
            }

            if (enableAlwaysOnCheck == true && isAlwaysOnDatabase)
            {
                await ChangeAvailabilityModeAsync(processId, server.Name, server.IntegratedSecurity, database.Name, true, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{processId}] An error occured while discovering indexes on {database} of {server}. Terminating the check of {database}");
            return;
        }
    }

    private async Task WaitForNextCheckToBecomeInactive(NextCheck nextCheck, CancellationToken cancellationToken)
    {
        bool active = true;

        while (active && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_executionIntervalSeconds * 1000);

            try
            {
                using (var dbConnection = _imsDbConectionFactory())
                {
                    dbConnection.Open();
                    active = await dbConnection.IsNextDbCheckCurrentlyHappening(nextCheck, _executionIntervalSeconds);
                    dbConnection.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while waiting for next check to become inactive for schedule id {nextCheck.ScheduleId}, db {nextCheck.DatabaseId}, server {nextCheck.ServerId}.");
            }
        }
    }

    private async Task<bool> ChangeAvailabilityModeAsync(Guid processId, string server, bool integratedSecurity, string database, bool setSynchronousCommit, CancellationToken cancellationToken)
    {
        using var clientConnection = _clientConnectionPool.GetMasterConnection(server, integratedSecurity);

        if (clientConnection == null || cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug($"[{processId}] Failed to acquire a connection to {server} and {database}. Terminating the check of {database}");
            return false;
        }

        clientConnection.Open();

        var isAlwasyonDatabase = await clientConnection!.IsDatabaseAlwaysonAndSynchronousAsync(database);
        bool? addOrRemoveAlwaysonDatabase = null;

        IEnumerable<string> sqls = new List<string>();

        if (isAlwasyonDatabase && !setSynchronousCommit)
        {
            _logger.LogInformation($"[{processId}] {database} on {server} is in synchronous_commit mode. Changing availability_mode to asynchronous_commit for all availability groups.");
            sqls = await clientConnection.GetAgSetSyncSqlsAsync(database);
            addOrRemoveAlwaysonDatabase = true;
        }
        else if (!isAlwasyonDatabase && setSynchronousCommit)
        {
            _logger.LogInformation($"[{processId}] {database} on {server} is in asynchronous_commit mode. Changing availability_mode to synchronous_commit for all availability groups.");
            sqls = await clientConnection.GetAgSqlsRevertAsync(database);
            addOrRemoveAlwaysonDatabase = false;
        }

        foreach (var sql in sqls)
        {
            await clientConnection.ExecuteAsync(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: cancellationToken));
        }

        clientConnection.Close();

        if (addOrRemoveAlwaysonDatabase.HasValue)
        {
            using var imsConnection = _imsDbConectionFactory();
            imsConnection.Open();

            if (addOrRemoveAlwaysonDatabase.Value)
            {
                await imsConnection.AddAlwaysonDatabaseAsync(server, database);
            }
            else
            {
                await imsConnection.RemoveAlwaysonDatabaseAsync(server, database);
            }

            imsConnection.Close();
        }

        return isAlwasyonDatabase;
    }
}
