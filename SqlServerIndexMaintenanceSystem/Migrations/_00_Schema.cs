namespace SqlServerIndexMaintenanceSystem.Migrations;

public static class _00_Schema
{
    public static string CreateTablesSql =
@$"CREATE TABLE [dbo].[ims_servers] (
    server_id INT PRIMARY KEY IDENTITY,
    [name] NVARCHAR(255) NOT NULL,
    discover_databases BIT NOT NULL DEFAULT 0,
    search_indexes BIT NOT NULL DEFAULT 0,
    
    max_threads INT NOT NULL DEFAULT 1,

    run_immediately BIT NOT NULL DEFAULT 0,
    schedule_id INT NULL,

    exclude_last_partition BIT NOT NULL DEFAULT 0,

    rebuild_threshold TINYINT NULL,
    reorganize_threshold TINYINT NULL,
    online BIT NULL,
    maxdop TINYINT NULL,
    sort_in_tempdb BIT NULL,
    index_min_size_kb INT NULL,

    tlog_size_factor FLOAT NULL,
    tlog_growth_factor FLOAT NULL,
    disk_safety_pct INT NULL,
    disk_min_remaining_mb INT NULL,
    enable_tlog_disk_check BIT NULL,
    enable_tlog_file_check BIT NULL,
    enable_always_on_check BIT NULL,

    integrated_security BIT NOT NULL DEFAULT 1,

    active BIT NOT NULL DEFAULT 1,
    CONSTRAINT UC_name UNIQUE ([name])
);

CREATE TABLE [dbo].[ims_databases] (
    database_id INT PRIMARY KEY IDENTITY,
    server_id INT NOT NULL,
    FOREIGN KEY (server_id) REFERENCES [dbo].[ims_servers](server_id),
    [name] NVARCHAR(255) NOT NULL,
    search_indexes BIT NULL,
    
    max_threads INT NOT NULL DEFAULT 1,

    run_immediately BIT NULL,
    schedule_id INT NULL,
    
    exclude_last_partition BIT NULL,

    rebuild_threshold TINYINT NULL,
    reorganize_threshold TINYINT NULL,
    online BIT NULL,
    maxdop TINYINT NULL,
    sort_in_tempdb BIT NULL,
    index_min_size_kb INT NULL,

    tlog_size_factor FLOAT NULL,
    tlog_growth_factor FLOAT NULL,

    enable_always_on_check BIT NULL,

    active BIT NOT NULL DEFAULT 1,
    CONSTRAINT UC_server_id_name UNIQUE (server_id, [name])
);

CREATE TABLE [dbo].[ims_indexes] (
    index_id INT PRIMARY KEY IDENTITY,
    database_id INT NOT NULL,
    FOREIGN KEY (database_id) REFERENCES [dbo].[ims_databases](database_id),
    
    [schema] NVARCHAR(255) NOT NULL,
    [table] NVARCHAR(255) NOT NULL,
    [name] NVARCHAR(255),
    
    run_immediately BIT NULL,
    schedule_id INT NULL,

    exclude_last_partition BIT NULL,

    rebuild_threshold TINYINT NULL,
    reorganize_threshold TINYINT NULL,
    online BIT NULL,
    maxdop TINYINT NULL,
    sort_in_tempdb BIT NULL,
    index_min_size_kb INT NULL,

    tlog_size_factor FLOAT NULL,
    tlog_growth_factor FLOAT NULL,

    active BIT NOT NULL DEFAULT 1,
    CONSTRAINT UC_database_id_schema_table_name UNIQUE (database_id, [schema], [table], [name])
);

CREATE TABLE [dbo].[ims_history_entries] (
    guid UNIQUEIDENTIFIER,
    insert_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reason NVARCHAR(255) NOT NULL,
    action NVARCHAR(255) NULL,
    [error] NVARCHAR(MAX) NULL,
    [server] NVARCHAR(255) NOT NULL,
    [database] NVARCHAR(255) NOT NULL,
    [schema] NVARCHAR(255) NOT NULL,
    [table] NVARCHAR(255) NOT NULL,
    [index] NVARCHAR(255) NOT NULL,
    object_id INT NULL,
    index_id INT NULL,
    rebuild_threshold TINYINT NULL,
    reorganize_threshold TINYINT NULL,
    online BIT NULL,
    maxdop TINYINT NULL,
    sort_in_tempdb BIT NULL,
    index_min_size_kb INT NULL,
    tlog_size_factor FLOAT NULL,
    tlog_growth_factor FLOAT NULL,
    disk_safety_pct INT NULL,
    disk_min_remaining_mb INT NULL,
    enable_tlog_disk_check BIT NULL,
    enable_tlog_file_check BIT NULL,
    enable_always_on_check BIT NULL,
    partition_number INT NULL,
    start_time DATETIME NULL,
    end_time DATETIME NULL,
    size_kb_before BIGINT NULL,
    size_kb_after BIGINT NULL,
    avg_fragmentation_percent_before TINYINT NULL,
    avg_fragmentation_percent_after TINYINT NULL,
    command NVARCHAR(MAX) NULL,
    CONSTRAINT UC_guid_partition_number UNIQUE (guid, partition_number)
);

CREATE TABLE [dbo].[ims_schedules] (
    schedule_id INT PRIMARY KEY IDENTITY,
    [name] NVARCHAR(255) NOT NULL,
    description NVARCHAR(255) NULL,
    freq_type INT NOT NULL,
    freq_interval INT NOT NULL,
    freq_subday_type INT NOT NULL,
    freq_subday_interval INT NOT NULL,
    freq_relative_interval INT NOT NULL,
    freq_recurrence_factor INT NOT NULL,
    active_start_date INT NOT NULL,
    active_end_date INT NOT NULL,
    active_start_time INT NOT NULL,
    active_end_time INT NOT NULL,
    date_created DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

ALTER TABLE [dbo].[ims_servers]
    ADD CONSTRAINT [FK_ims_servers_ims_schedules]
    FOREIGN KEY (schedule_id)
    REFERENCES [dbo].[ims_schedules](schedule_id);
ALTER TABLE [dbo].[ims_databases]
    ADD CONSTRAINT [FK_ims_databases_ims_schedules]
    FOREIGN KEY (schedule_id)
    REFERENCES [dbo].[ims_schedules](schedule_id);
ALTER TABLE [dbo].[ims_indexes]
    ADD CONSTRAINT [FK_ims_indexes_ims_schedules]
    FOREIGN KEY (schedule_id)
    REFERENCES [dbo].[ims_schedules](schedule_id);

CREATE TABLE [dbo].[ims_next_checks] (
    schedule_id INT NOT NULL,
    FOREIGN KEY (schedule_id) REFERENCES [dbo].[ims_schedules](schedule_id) ON DELETE CASCADE,
    server_id INT NOT NULL,  
    FOREIGN KEY (server_id) REFERENCES [dbo].[ims_servers](server_id) ON DELETE CASCADE,
    database_id INT NULL,
    FOREIGN KEY (database_id) REFERENCES [dbo].[ims_databases](database_id) ON DELETE CASCADE,
    index_id INT NULL,
    FOREIGN KEY (index_id) REFERENCES [dbo].[ims_indexes](index_id) ON DELETE CASCADE,
    previous_execution_date INT NULL,
    previous_execution_time INT NULL,
    next_execution_date INT NULL,
    next_execution_time INT NULL,
    CONSTRAINT UC_schedule_id_server_id_database_id_index_id UNIQUE (schedule_id, server_id, database_id, index_id)
);

CREATE TABLE [dbo].[ims_alwayson_databases] (
    database_id INT PRIMARY KEY,
    FOREIGN KEY (database_id) REFERENCES [dbo].[ims_databases](database_id)
);
";
}
