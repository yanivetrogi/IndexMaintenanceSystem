-- Diagnostic Queries for Index Maintenance System
-- Run these against the IndexMaintenanceSystem database

-- 1. Check Server Configuration
SELECT 
    server_id,
    name,
    active,
    discover_databases,
    search_indexes,
    schedule_id,
    max_threads
FROM ims_servers
WHERE name = 'YOUR_SERVER_NAME';

-- 2. Check Database Configuration
SELECT 
    database_id,
    server_id,
    name,
    active,
    search_indexes,
    schedule_id,
    run_immediately
FROM ims_databases
WHERE name = 'YOUR_DATABASE_NAME';

-- 3. Check if there are any indexes configured for DevOpsTest
SELECT 
    i.index_id,
    i.database_id,
    i.[schema],
    i.[table],
    i.[name],
    i.active,
    i.schedule_id
FROM ims_indexes i
INNER JOIN ims_databases d ON d.database_id = i.database_id
WHERE d.name = 'YOUR_DATABASE_NAME';

-- 4. Check next_checks for DevOpsTest
SELECT 
    nc.server_id,
    nc.database_id,
    nc.index_id,
    nc.schedule_id,
    nc.next_execution_date,
    nc.next_execution_time,
    nc.previous_execution_date,
    nc.previous_execution_time,
    s.name as server_name,
    d.name as database_name
FROM ims_next_checks nc
LEFT JOIN ims_servers s ON s.server_id = nc.server_id
LEFT JOIN ims_databases d ON d.database_id = nc.database_id
WHERE d.name = 'YOUR_DATABASE_NAME' OR s.name = 'YOUR_SERVER_NAME';

-- 5. Check schedule details
SELECT 
    schedule_id,
    name,
    freq_type,
    freq_interval,
    active_start_date,
    active_start_time,
    active_end_date,
    active_end_time
FROM ims_schedules
ORDER BY schedule_id DESC;
