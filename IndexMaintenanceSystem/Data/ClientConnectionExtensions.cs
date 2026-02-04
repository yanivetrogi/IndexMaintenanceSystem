using IndexMaintenanceSystem.Models.Client;
using Dapper;
using Index = IndexMaintenanceSystem.Models.Ims.Index;
using System.Data;
using System.Text;

namespace IndexMaintenanceSystem.Data;

public static class ClientConnectionExtensions
{
    public static async Task<IndexSysInfo?> GetIndexSysInfoAsync(this IDbConnection _connection, Index index)
    {
        var query =
@$"SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 
SELECT
    p.object_id,
    p.index_id,
    MAX(p.partition_number) partition_count
FROM
    sys.partitions p
    INNER JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
WHERE
    p.object_id = OBJECT_ID(@TableFullName)
    AND i.name = @Name
GROUP BY p.object_id, p.index_id";

        return await _connection.QuerySingleOrDefaultAsync<IndexSysInfo>(query, new { index.Name, TableFullName = $"{index.Schema}.{index.Table}" });
    }

    public static async Task<IndexDefragInfo> GetIndexDefragInfoAsync(
        this IDbConnection _connection,
        Index index,
        IndexSysInfo sysInfo,
        int partitionNumber,
        byte? rebuildThreshold = null,
        byte? reorganizeThreshold = null,
        bool? online = null,
        byte? maxdop = null,
        bool? sortInTempdb = null,
        bool buildCommand = false)
    {
        var query = @$"SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 
EXECUTE [dbo].[sp_index_defrag_info]
@object_id={sysInfo.ObjectId}
,@index_id={sysInfo.IndexId}
,@schema_name=""{index.Schema}""
,@table_name=""{index.Table}""
,@index_name=""{index.Name}""";

        var sb = new StringBuilder(query);

        if (sysInfo.PartitionCount > 1) sb.AppendLine($",@partition_number={partitionNumber}");
        if (sysInfo.PartitionCount > 1) sb.AppendLine(",@has_multiple_partitions=1");
        if (rebuildThreshold.HasValue) sb.AppendLine($",@rebuild_threshold={rebuildThreshold}");
        if (reorganizeThreshold.HasValue) sb.AppendLine($",@reorg_threshold={reorganizeThreshold}");
        if (online.HasValue) sb.AppendLine($",@online={(online.Value ? '1' : '0')}");
        if (maxdop.HasValue) sb.AppendLine($",@maxdop={maxdop}");
        if (sortInTempdb.HasValue) sb.AppendLine($",@sort_in_tempdb={(sortInTempdb.Value ? '1' : '0')}");
        if (buildCommand) sb.AppendLine(",@build_command=1");

        return await _connection.QueryFirstAsync<IndexDefragInfo>(sb.ToString(), commandTimeout: 0);
    }

    public static async Task<IEnumerable<DiscoveredIndex>> DiscoverIndexesAsync(this IDbConnection _connection)
    {
        var query =
@"SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 
SELECT SCHEMA_NAME(t.schema_id) AS [schema],
    t.name AS [table],
    i.name as [name]
FROM sys.tables t
INNER JOIN sys.indexes i ON t.[object_id] = i.[object_id]
WHERE i.type in (1, 2) AND i.is_disabled = 0 AND i.is_hypothetical = 0
ORDER BY NEWID()";

        return await _connection.QueryAsync<DiscoveredIndex>(query);
    }

    public static async Task<IEnumerable<DiscoveredDatabase>> DiscoverDatabasesAsync(this IDbConnection _connection)
    {
        var query =
@"SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 
SELECT [name]
FROM master.sys.databases
WHERE (CASE WHEN [name] IN ('master','model','msdb','tempdb') THEN 1 ELSE is_distributor END) = 0
    AND state_desc = N'ONLINE'
    AND DATABASEPROPERTYEX([name] , N'Updateability') = N'READ_WRITE'";

        return await _connection.QueryAsync<DiscoveredDatabase>(query);
    }

    public static async Task<int> GetTransactionLogSizeMbAsync(this IDbConnection _connection)
    {
        var query = $"SELECT SUM(f.size / 128) size_mb FROM sys.database_files f WHERE f.type_desc LIKE N'LOG'";

        return await _connection.QuerySingleAsync<int>(query);
    }

    public static async Task<int> GetTransactionLogAvailableSizeMbAsync(this IDbConnection _connection)
    {
        var query =
@$"SELECT vs.available_bytes / 1024 / 1024 as free_space_mb
FROM sys.database_files f
CROSS APPLY sys.dm_os_volume_stats(DB_ID(), f.file_id) vs
WHERE f.type_desc LIKE N'LOG'";

        return await _connection.QuerySingleAsync<int>(query);
    }

    public static async Task<bool> IsDatabaseAlwaysonAndSynchronousAsync(this IDbConnection _connection, string database)
    {
        var query =
@$"SELECT COUNT(*)
FROM sys.dm_hadr_database_replica_states drs
INNER JOIN sys.availability_replicas ar ON drs.replica_id = ar.replica_id
WHERE drs.database_id = DB_ID(@Database)
    AND synchronization_state_desc = N'SYNCHRONIZED'
    AND drs.synchronization_health_desc = N'HEALTHY'
    AND database_state_desc = N'ONLINE'
    AND drs.is_local = 1";

        return await _connection.ExecuteScalarAsync<int>(query, new { Database = database }) > 0;
    }

    public static async Task<IEnumerable<string>> GetAgSetSyncSqlsAsync(this IDbConnection _connection, string database)
    {
        var query =
@$"
SELECT
       'ALTER AVAILABILITY GROUP [' + ag.name + '] MODIFY REPLICA ON N''' + ar.replica_server_name + ''' WITH (AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT);'
FROM sys.dm_hadr_database_replica_states drs
INNER JOIN sys.availability_groups AS ag ON ag.group_id = drs.group_id
INNER JOIN sys.availability_replicas AS ar ON drs.group_id = ar.group_id AND drs.replica_id = ar.replica_id
WHERE DB_NAME(drs.database_id) = @Database
AND drs.is_primary_replica = 1
AND drs.synchronization_state_desc = N'SYNCHRONIZED'
";

        return await _connection.QueryAsync<string>(query, new { Database = database });
    }

    public static async Task<IEnumerable<string>> GetAgSqlsRevertAsync(this IDbConnection _connection, string database)
    {
        var query =
@$"
SELECT
       'ALTER AVAILABILITY GROUP [' + ag.name + '] MODIFY REPLICA ON N''' + ar.replica_server_name + ''' WITH (AVAILABILITY_MODE = SYNCHRONOUS_COMMIT);'
FROM sys.dm_hadr_database_replica_states drs
INNER JOIN sys.availability_groups AS ag ON ag.group_id = drs.group_id
INNER JOIN sys.availability_replicas AS ar ON drs.group_id = ar.group_id AND drs.replica_id = ar.replica_id
WHERE DB_NAME(drs.database_id) = @Database
AND drs.is_primary_replica = 1
AND drs.synchronization_state_desc = N'SYNCHRONIZED'
";

        return await _connection.QueryAsync<string>(query, new { Database = database });
    }
}
