using Dapper;
using IndexMaintenanceSystem.Models.Ims;
using System.Data;
using IndexModel = IndexMaintenanceSystem.Models.Ims.Index;

namespace IndexMaintenanceSystem.Services;

public class DashboardService
{
    private readonly ImsConnectionFactory _connectionFactory;

    public DashboardService(ImsConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Server>> GetServersAsync()
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryAsync<Server>("SELECT * FROM ims_servers");
    }

    public async Task<IEnumerable<Database>> GetDatabasesAsync()
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryAsync<Database>("SELECT * FROM ims_databases");
    }

    public async Task<IEnumerable<Schedule>> GetSchedulesAsync()
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryAsync<Schedule>("SELECT * FROM ims_schedules");
    }

    public async Task<IEnumerable<NextCheckDisplay>> GetUpcomingChecksAsync()
    {
        // Joining tables to get readable names
        var sql = @"
            SELECT 
                dbo.ims_agent_datetime(nc.next_execution_date, nc.next_execution_time) as NextExecution,
                s.name as ServerName,
                d.name as DatabaseName,
                i.name as IndexName,
                sch.name as ScheduleName
            FROM ims_next_checks nc
            JOIN ims_servers s ON nc.server_id = s.server_id
            LEFT JOIN ims_databases d ON nc.database_id = d.database_id
            LEFT JOIN ims_indexes i ON nc.index_id = i.index_id
            LEFT JOIN ims_schedules sch ON nc.schedule_id = sch.schedule_id
            ORDER BY dbo.ims_agent_datetime(nc.next_execution_date, nc.next_execution_time)
            OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY";

        using var connection = _connectionFactory();
        connection.Open();
            return await connection.QueryAsync<NextCheckDisplay>(sql);
    }

    public async Task<Server?> GetServerAsync(int id)
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryFirstOrDefaultAsync<Server>("SELECT * FROM ims_servers WHERE server_id = @id", new { id });
    }

    public async Task UpdateServerAsync(Server server)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            UPDATE ims_servers SET 
                name = @Name, max_threads = @MaxThreads, discover_databases = @DiscoverDatabases, 
                search_indexes = @SearchIndexes, schedule_id = @ScheduleId, exclude_last_partition = @ExcludeLastPartition,
                run_immediately = @RunImmediately, rebuild_threshold = @RebuildThreshold, reorganize_threshold = @ReorganizeThreshold,
                online = @Online, maxdop = @Maxdop, sort_in_tempdb = @SortInTempdb, index_min_size_kb = @IndexMinSizeKb,
                tlog_size_factor = @TlogSizeFactor, tlog_growth_factor = @TlogGrowthFactor, disk_safety_pct = @DiskSafetyPct,
                disk_min_remaining_mb = @DiskMinRemainingMb, enable_tlog_disk_check = @EnableTlogDiskCheck, 
                enable_tlog_file_check = @EnableTlogFileCheck, enable_always_on_check = @EnableAlwaysOnCheck,
                integrated_security = @IntegratedSecurity, active = @Active
            WHERE server_id = @ServerId", server);
    }

    public async Task AddServerAsync(Server server)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            INSERT INTO ims_servers (
                name, max_threads, discover_databases, search_indexes, schedule_id, exclude_last_partition,
                run_immediately, rebuild_threshold, reorganize_threshold, online, maxdop, sort_in_tempdb, index_min_size_kb,
                tlog_size_factor, tlog_growth_factor, disk_safety_pct, disk_min_remaining_mb, enable_tlog_disk_check, 
                enable_tlog_file_check, enable_always_on_check, integrated_security, active
            ) VALUES (
                @Name, @MaxThreads, @DiscoverDatabases, @SearchIndexes, @ScheduleId, @ExcludeLastPartition,
                @RunImmediately, @RebuildThreshold, @ReorganizeThreshold, @Online, @Maxdop, @SortInTempdb, @IndexMinSizeKb,
                @TlogSizeFactor, @TlogGrowthFactor, @DiskSafetyPct, @DiskMinRemainingMb, @EnableTlogDiskCheck, 
                @EnableTlogFileCheck, @EnableAlwaysOnCheck, @IntegratedSecurity, @Active
            )", server);
    }

    public async Task<Database?> GetDatabaseAsync(int id)
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryFirstOrDefaultAsync<Database>("SELECT * FROM ims_databases WHERE database_id = @id", new { id });
    }

    public async Task UpdateDatabaseAsync(Database database)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            UPDATE ims_databases SET 
                name = @Name, max_threads = @MaxThreads, search_indexes = @SearchIndexes, schedule_id = @ScheduleId,
                exclude_last_partition = @ExcludeLastPartition, run_immediately = @RunImmediately, 
                rebuild_threshold = @RebuildThreshold, reorganize_threshold = @ReorganizeThreshold, online = @Online, 
                maxdop = @Maxdop, sort_in_tempdb = @SortInTempdb, index_min_size_kb = @IndexMinSizeKb, 
                tlog_size_factor = @TlogSizeFactor, tlog_growth_factor = @TlogGrowthFactor, 
                enable_always_on_check = @EnableAlwaysOnCheck, active = @Active
            WHERE database_id = @DatabaseId", database);
    }

    public async Task<Schedule?> GetScheduleAsync(int id)
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryFirstOrDefaultAsync<Schedule>("SELECT * FROM ims_schedules WHERE schedule_id = @id", new { id });
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            UPDATE ims_schedules SET 
                name = @Name, description = @Description, freq_type = @FreqType, freq_interval = @FreqInterval, 
                freq_subday_type = @FreqSubdayType, freq_subday_interval = @FreqSubdayInterval, 
                freq_relative_interval = @FreqRelativeInterval, freq_recurrence_factor = @FreqRecurrenceFactor, 
                active_start_date = @ActiveStartDate, active_end_date = @ActiveEndDate, 
                active_start_time = @ActiveStartTime, active_end_time = @ActiveEndTime
            WHERE schedule_id = @ScheduleId", schedule);
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            INSERT INTO ims_schedules (
                name, description, freq_type, freq_interval, freq_subday_type, freq_subday_interval, 
                freq_relative_interval, freq_recurrence_factor, active_start_date, active_end_date, 
                active_start_time, active_end_time, date_created
            ) VALUES (
                @Name, @Description, @FreqType, @FreqInterval, @FreqSubdayType, @FreqSubdayInterval, 
                @FreqRelativeInterval, @FreqRecurrenceFactor, @ActiveStartDate, @ActiveEndDate, 
                @ActiveStartTime, @ActiveEndTime, GETDATE()
            )", schedule);
    }

    public async Task<IEnumerable<IndexModel>> GetIndexesAsync()
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryAsync<IndexModel>("SELECT * FROM ims_indexes ORDER BY database_id, [schema], [table], [name]");
    }

    public async Task<IndexModel?> GetIndexAsync(int id)
    {
        using var connection = _connectionFactory();
        connection.Open();
        return await connection.QueryFirstOrDefaultAsync<IndexModel>("SELECT * FROM ims_indexes WHERE index_id = @id", new { id });
    }

    public async Task UpdateIndexAsync(IndexModel index)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            UPDATE ims_indexes SET 
                [schema] = @Schema, [table] = @Table, [name] = @Name, active = @Active, 
                schedule_id = @ScheduleId, run_immediately = @RunImmediately, 
                rebuild_threshold = @RebuildThreshold, reorganize_threshold = @ReorganizeThreshold, 
                online = @Online, maxdop = @Maxdop, sort_in_tempdb = @SortInTempdb, 
                index_min_size_kb = @IndexMinSizeKb, exclude_last_partition = @ExcludeLastPartition,
                tlog_size_factor = @TlogSizeFactor, tlog_growth_factor = @TlogGrowthFactor
            WHERE index_id = @IndexId", index);
    }

    public async Task AddIndexAsync(IndexModel index)
    {
        using var connection = _connectionFactory();
        connection.Open();
        await connection.ExecuteAsync(@"
            INSERT INTO ims_indexes (
                database_id, [schema], [table], [name], active, schedule_id, run_immediately, 
                rebuild_threshold, reorganize_threshold, online, maxdop, sort_in_tempdb, 
                index_min_size_kb, exclude_last_partition, tlog_size_factor, tlog_growth_factor
            ) VALUES (
                @DatabaseId, @Schema, @Table, @Name, @Active, @ScheduleId, @RunImmediately, 
                @RebuildThreshold, @ReorganizeThreshold, @Online, @Maxdop, @SortInTempdb, 
                @IndexMinSizeKb, @ExcludeLastPartition, @TlogSizeFactor, @TlogGrowthFactor
            )", index);
    }
}

public class NextCheckDisplay
{
    public DateTime NextExecution { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string? DatabaseName { get; set; }
    public string? IndexName { get; set; }
    public string? ScheduleName { get; set; }
}

