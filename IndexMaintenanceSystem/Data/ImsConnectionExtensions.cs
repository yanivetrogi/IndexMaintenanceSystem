using System.Data;
using Dapper;
using IndexMaintenanceSystem.Models.Ims;
using Index = IndexMaintenanceSystem.Models.Ims.Index;
using IndexMaintenanceSystem.Models.Client;

namespace IndexMaintenanceSystem.Data;

public static class ImsConnectionExtensions
{
    private static SemaphoreSlim lockingSemaphore = new(1);

    // plan_next_check stored procedure is blocking next_checks table.
    // this procedure is also called when inserting ims objects.
    // therefore, all inserts and direct calls are wrapped in lock to be sequential.
    private static async Task<T> InLock<T>(Func<Task<T>> func)
    {
        await lockingSemaphore.WaitAsync();
        try
        {
            return await func();
        }
        finally
        {
            lockingSemaphore.Release();
        }
    }

    public static async Task InsertIndexesAsync(this IDbConnection _connection, IEnumerable<DiscoveredIndex> indexes, bool active)
    {
        var query =
$@"IF NOT EXISTS (SELECT 1 FROM ims_indexes WHERE database_id = @DatabaseId AND [schema] = @Schema AND [table] = @Table AND [name] = @Name)
INSERT
    INTO ims_indexes (database_id, [schema], [table], [name], active)
    VALUES (@DatabaseId, @Schema, @Table, @Name, {(active ? '1' : '0')})";
        await InLock(async () => await _connection.ExecuteAsync(query, indexes));
    }


    public static async Task InsertDatabasesAsync(this IDbConnection _connection, IEnumerable<DiscoveredDatabase> databases)
    {
        var query =
$@"IF NOT EXISTS (SELECT 1 FROM ims_databases WHERE server_id = @ServerId AND [name] = @Name)
INSERT
    INTO ims_databases (server_id, [name])
    VALUES (@ServerId, @Name)";
        await InLock(async () => await _connection.ExecuteAsync(query, databases));
    }

    public static async Task InsertHistoryEntryAsync(this IDbConnection _connection, HistoryEntry historyEntry)
    {
        await _connection.ExecuteAsync(
    @"INSERT INTO [dbo].[ims_history_entries]
        ([guid],
        [server],
        [reason],
        [action],
        [error],
        [database],
        [schema],
        [table],
        [index],
        [object_id],
        [index_id],
        [rebuild_threshold],
        [reorganize_threshold],
        [online],
        [maxdop],
        [sort_in_tempdb],
        [index_min_size_kb],
        [partition_number],
        [start_time],
        [end_time],
        [size_kb_before],
        [size_kb_after],
        [avg_fragmentation_percent_before],
        [avg_fragmentation_percent_after],
        [command])
    VALUES
        (@Guid,
        @Server,
        @Reason,
        @Action,
        @Error,
        @Database,
        @Schema,
        @Table,
        @Index,
        @ObjectId,
        @IndexId,
        @RebuildThreshold,
        @ReorganizeThreshold,
        @Online,
        @Maxdop,
        @SortInTempdb,
        @IndexMinSizeKb,
        @PartitionNumber,
        @StartTime,
        @EndTime,
        @SizeKBBefore,
        @SizeKBAfter,
        @AvgFragmentationPercentBefore,
        @AvgFragmentationPercentAfter,
        @Command)",
            historyEntry);
    }

    public static async Task UpdateHistoryEntryWithEndValuesAsync(this IDbConnection _connection, HistoryEntry historyEntry)
    {
        var andPartitionClause = historyEntry.PartitionNumber.HasValue ? "AND partition_number = @PartitionNumber" : "";
        await _connection.ExecuteAsync(
$@"UPDATE [dbo].[ims_history_entries]
SET
    start_time = @StartTime,
    end_time = @EndTime,
    size_kb_after = @SizeKBAfter,
    avg_fragmentation_percent_after = @AvgFragmentationPercentAfter,
    error = @Error
WHERE guid = @Guid {andPartitionClause}",
            historyEntry);
    }

    public static async Task<NextCheck?> PlanNextCheckAsync(this IDbConnection _connection, Schedule? schedule, Server server, Database? database = null, Index? index = null)
    {
        return await InLock(async () => await _connection.QuerySingleOrDefaultAsync<NextCheck>(
            "[dbo].[sp_ims_plan_next_check]",
            new
            {
                server_id = server.ServerId,
                database_id = database?.DatabaseId,
                index_id = index?.IndexId,
                schedule_id = schedule?.ScheduleId,
            },
            commandType: CommandType.StoredProcedure
        ));
    }

    public static async Task<NextCheck?> RescheduleNextCheckIfExistsAsync(this IDbConnection _connection, NextCheck nextCheck)
    {

        return await InLock(async () =>
        {
            var existsQuery =
@$"SELECT CAST(CASE WHEN COUNT(*) = 0 THEN 0 ELSE 1 END AS BIT)
FROM [dbo].[ims_next_checks] nc
WHERE nc.schedule_id = @ScheduleId
    AND nc.server_id = @ServerId
    AND (nc.database_id = @DatabaseId OR (nc.database_id IS NULL AND @DatabaseId IS NULL))
    AND (nc.index_id = @IndexId OR (nc.index_id IS NULL AND @IndexId IS NULL))";

            var exists = await _connection.QueryFirstAsync<bool>(existsQuery, nextCheck);

            if (!exists)
            {
                return null;
            }
                
            var setPrevExecQuery =
@"UPDATE [dbo].[ims_next_checks]
SET previous_execution_date = @NextExecutionDate, previous_execution_time = @NextExecutionTime
WHERE server_id = @ServerId
    AND ((database_id IS NULL and @DatabaseId IS NULL) OR database_id = @DatabaseId)
    AND ((index_id IS NULL AND @IndexId IS NULL) OR index_id = @IndexId)
    AND ((schedule_id IS NULL and @ScheduleId IS NULL) OR schedule_id = @ScheduleId);";

            await _connection.ExecuteAsync(setPrevExecQuery, nextCheck);

            return await _connection.QuerySingleOrDefaultAsync<NextCheck>(
                "[dbo].[sp_ims_plan_next_check]",
                new
                {
                    server_id = nextCheck.ServerId,
                    database_id = nextCheck.DatabaseId,
                    index_id = nextCheck.IndexId,
                    schedule_id = nextCheck.ScheduleId,
                },
                commandType: CommandType.StoredProcedure
            );
        });
    }

    /// <remarks>
    /// A check is considered active if its next execution time is either
    /// in the past or scheduled within the specified validity interval from now into the future.
    /// </remarks>
    public static async Task<bool> IsNextDbCheckCurrentlyHappening(this IDbConnection _connection, NextCheck nextCheck, int validityIntervalSeconds)
    {
        var query =
$@"SELECT COUNT(*) FROM [dbo].[ims_next_checks] nc
WHERE nc.schedule_id = @ScheduleId
    AND nc.server_id = @ServerId
    AND nc.database_id = @DatabaseId
    AND nc.index_id IS NULL
    AND [dbo].[ims_agent_datetime](nc.next_execution_date, nc.next_execution_time) <= DATEADD(HOUR, 12, CURRENT_TIMESTAMP)";

        return await _connection.QuerySingleAsync<int>(query, nextCheck) > 0;
    }

    /// <remarks>
    /// Returns list of indexes to be processed due to index's schedule or run_immediately.
    /// </remarks>
    public static async Task<IList<(Server, Database, Index, NextCheck?, Schedule?)>> GetNextIndexesToProcessAsync(this IDbConnection _connection, int validityIntervalSeconds)
    {
        var query =
$@"SELECT s.*, d.*, i.*, nch.*, sch.* FROM
[dbo].[ims_servers] s
INNER JOIN [dbo].[ims_databases] d ON s.server_id = d.server_id
INNER JOIN [dbo].[ims_indexes] i ON d.database_id = i.database_id
LEFT JOIN [dbo].[ims_next_checks] nch ON nch.server_id = s.server_id AND nch.database_id = d.database_id AND nch.index_id = i.index_id AND (i.run_immediately IS NULL OR i.run_immediately = 0)
LEFT JOIN [dbo].[ims_schedules] sch ON nch.schedule_id = sch.schedule_id
WHERE s.active = 1 AND d.active = 1 AND i.active = 1
    AND i.[name] IS NOT NULL AND i.[name] <> ''
    AND (i.run_immediately = 1
        OR ([dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) <= CURRENT_TIMESTAMP
            AND [dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) >= DATEADD(SECOND, -{validityIntervalSeconds}, CURRENT_TIMESTAMP)))";

        var result = await _connection.QueryAsync<Server, Database, Index, NextCheck?, Schedule?, (Server, Database, Index, NextCheck?, Schedule?)>(
            query,
            (s, d, i, nch, sch) => (s, d, i, nch, sch),
            splitOn: "database_id, index_id, schedule_id, schedule_id"
        );

        return result.ToList();
    }

    /// <remarks>
    /// Returns list of databases to be processed due to schedule or run_immediately.
    /// Contains a list of all index-overrides
    /// Inactive indexes or indexes with own schedule are also fetched in order to be filtered out after discovery.
    /// </remarks>
    public static async Task<IList<(Server, Database, List<Index>, NextCheck?, Schedule?)>> GetNextDbsToProcessAsync(this IDbConnection _connection, int validityIntervalSeconds)
    {
        var query =
$@"SELECT s.*, d.*, nch.*, i.*, sch.* FROM
[dbo].[ims_servers] s
INNER JOIN [dbo].[ims_databases] d ON s.server_id = d.server_id
LEFT JOIN [dbo].[ims_indexes] i ON d.database_id = i.database_id
LEFT JOIN [dbo].[ims_next_checks] nch ON nch.server_id = s.server_id AND nch.database_id = d.database_id AND (d.run_immediately IS NULL OR d.run_immediately = 0)
LEFT JOIN [dbo].[ims_schedules] sch ON nch.schedule_id = sch.schedule_id
WHERE s.active = 1 AND d.active = 1
    AND nch.index_id IS NULL
    AND (d.run_immediately = 1
        OR ([dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) <= CURRENT_TIMESTAMP
            AND [dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) >= DATEADD(SECOND, -{validityIntervalSeconds}, CURRENT_TIMESTAMP)))";

        var result = await _connection.QueryAsync<Server, Database, NextCheck?, Index?, Schedule?, (Server, Database, NextCheck?, Index?, Schedule?)>(
            query,
            (s, d, nch, i, sch) => (s, d, nch, i, sch),
            splitOn: "database_id, schedule_id, index_id, schedule_id"
        );

        return result.GroupBy(e => e.Item2.DatabaseId).Select(group =>
            {
                var groupItems = group.ToList();

                return (
                    Server: groupItems[0].Item1,
                    Database: groupItems[0].Item2,
                    IndexOverrides: groupItems[0].Item4 != null
                        ? groupItems.Select(item => item.Item4!).ToList()
                        : new List<Index>(),
                    NextCheck: groupItems[0].Item3,
                    Schedule: groupItems[0].Item5
                );
            }).ToList();
    }

    public static async Task<IEnumerable<(Server, NextCheck?, Schedule?)>> GetServersToProcessAsync(this IDbConnection _connection, int validityIntervalSeconds)
    {
        var query =
$@"SELECT s.*, nch.*, sch.* FROM
[dbo].[ims_servers] s
LEFT JOIN [dbo].[ims_next_checks] nch ON s.server_id = nch.server_id AND (s.run_immediately IS NULL OR s.run_immediately = 0)
LEFT JOIN [dbo].[ims_schedules] sch ON nch.schedule_id = sch.schedule_id
WHERE nch.database_id IS NULL AND nch.index_id IS NULL
    AND s.active = 1
    AND (s.run_immediately = 1
        OR ([dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) <= CURRENT_TIMESTAMP
            AND [dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) >= DATEADD(SECOND, -{validityIntervalSeconds}, CURRENT_TIMESTAMP)))";

        return await _connection.QueryAsync<Server, NextCheck?, Schedule?, (Server, NextCheck?, Schedule?)>(
            query,
            (s, nch, sch) => (s, nch, sch),
            splitOn: "schedule_id, schedule_id"
        );
    }

    public static async Task<IEnumerable<(Server, Database?, Index?, Schedule?, NextCheck)>> GetObsoleteChecksAsync(this IDbConnection _connection, int validityIntervalSeconds)
    {
        var query =
$@"SELECT s.*, d.*, i.*, sch.*, nch.* FROM
[dbo].[ims_next_checks] nch
INNER JOIN [dbo].[ims_servers] s ON nch.server_id = s.server_id
LEFT JOIN [dbo].[ims_databases] d ON nch.database_id = d.database_id
LEFT JOIN [dbo].[ims_indexes] i ON nch.index_id = i.index_id
LEFT JOIN [dbo].[ims_schedules] sch ON nch.schedule_id = sch.schedule_id
WHERE 
    s.active = 1
    AND (d.active IS NULL OR d.active = 1)
    AND (i.active IS NULL OR i.active = 1)
    AND [dbo].[ims_agent_datetime](nch.next_execution_date, nch.next_execution_time) < DATEADD(SECOND, -{validityIntervalSeconds}, CURRENT_TIMESTAMP)";

        return await _connection.QueryAsync<Server, Database?, Index?, Schedule?, NextCheck, (Server, Database?, Index?, Schedule?, NextCheck)>(
            query,
            (s, d, i, sch, nch) => (s, d, i, sch, nch),
            splitOn: "database_id, index_id, schedule_id, schedule_id"
        );
    }

    public static async Task InactivateIndexAsync(this IDbConnection _connection, Index index)
    {
        var query = @$"UPDATE ims_indexes SET active = 0 WHERE index_id = @IndexId";
        await InLock(async () => await _connection.ExecuteAsync(query, index));
    }

    public static async Task TurnOffDatabaseDiscoveryAsync(this IDbConnection _connection, Server server)
    {
        var query = @$"UPDATE ims_servers SET discover_databases = 0 WHERE server_id = @ServerId";
        await InLock(async () => await _connection.ExecuteAsync(query, server));
    }

    public static async Task TurnOffRunImmediatelyAsync(this IDbConnection _connection, Server server)
    {
        var query = @$"UPDATE ims_servers SET run_immediately = 0 WHERE server_id = @ServerId";
        await InLock(async () => await _connection.ExecuteAsync(query, server));
    }

    public static async Task TurnOffRunImmediatelyAsync(this IDbConnection _connection, Database database)
    {
        var query = @$"UPDATE ims_databases SET run_immediately = NULL WHERE database_id = @DatabaseId";
        await InLock(async () => await _connection.ExecuteAsync(query, database));
    }

    public static async Task TurnOffRunImmediatelyAsync(this IDbConnection _connection, Index index)
    {
        var query = @$"UPDATE ims_indexes SET run_immediately = NULL WHERE index_id = @IndexId";
        await InLock(async () => await _connection.ExecuteAsync(query, index));
    }

    public static async Task TurnOnRunImmediatelyOfChildDatabasesAsync(this IDbConnection _connection, Server server)
    {
        var query = @$"UPDATE ims_databases SET run_immediately = 1 WHERE server_id = @ServerId AND run_immediately IS NULL AND schedule_id IS NULL";
        await InLock(async () => await _connection.ExecuteAsync(query, server));
    }

    public static async Task<IEnumerable<DatabaseAlwayson>> GetDatabasesWithAlwaysonAsync(this IDbConnection _connection)
    {
        var query =
@"SELECT s.[name] AS [Server], s.[integrated_security] AS [IntegratedSecurity], d.[name] AS [Database]
FROM ims_alwayson_databases ad
join ims_databases d on ad.database_id = d.database_id
join ims_servers s on d.server_id = s.server_id";
        return await _connection.QueryAsync<DatabaseAlwayson>(query);
    }

    public static async Task AddAlwaysonDatabaseAsync(this IDbConnection _connection, string server, string database)
    {
        var query = @$"INSERT INTO ims_alwayson_databases (database_id)
VALUES ((SELECT d.database_id FROM ims_databases d
    JOIN ims_servers s ON d.server_id = s.server_id
    WHERE s.[name] = @Server AND d.[name] = @Database))";
        await InLock(async () => await _connection.ExecuteAsync(query, new { Server = server, Database = database }));
    }

    public static async Task RemoveAlwaysonDatabaseAsync(this IDbConnection _connection, string server, string database)
    {
        var query =
@$"DELETE FROM ims_alwayson_databases
WHERE database_id = (
    SELECT d.database_id
    FROM ims_databases d
    JOIN ims_servers s ON d.server_id = s.server_id
    WHERE s.[name] = @Server AND d.[name] = @Database)";
        await InLock(async () => await _connection.ExecuteAsync(query, new { Server = server, Database = database }));
    }
}
