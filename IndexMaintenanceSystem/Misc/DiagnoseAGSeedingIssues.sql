-- =============================================
-- AlwaysOn Availability Group Seeding Diagnostics
-- Focus: Databases with seeding failures (error 0x000005b4)
-- =============================================

SET NOCOUNT ON;

PRINT '========================================';
PRINT 'AG SEEDING FAILURE DIAGNOSTICS';
PRINT 'Generated: ' + CONVERT(VARCHAR(30), GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- List of databases that had seeding failures
DECLARE @FailedDatabases TABLE (DatabaseName NVARCHAR(128));
INSERT INTO @FailedDatabases VALUES
    ('YourDatabase1'),
    ('YourDatabase2'),
    ('YourDatabase3');

-- =============================================
-- 1. Current Database States
-- =============================================
PRINT '1. DATABASE STATES (Databases with seeding failures)';
PRINT '----------------------------------------------------';

SELECT 
    d.name AS DatabaseName,
    d.state_desc AS DatabaseState,
    d.recovery_model_desc AS RecoveryModel,
    CAST(d.log_reuse_wait_desc AS VARCHAR(30)) AS LogReuseWait,
    CASE 
        WHEN hdrs.database_id IS NOT NULL THEN 'In AG'
        ELSE 'NOT in AG'
    END AS AGStatus
FROM sys.databases d
LEFT JOIN sys.dm_hadr_database_replica_states hdrs ON d.database_id = hdrs.database_id
WHERE d.name IN (SELECT DatabaseName FROM @FailedDatabases)
ORDER BY d.name;

PRINT '';
PRINT '';

-- =============================================
-- 2. AG Replica Synchronization Status
-- =============================================
PRINT '2. AG SYNCHRONIZATION STATUS';
PRINT '----------------------------------------------------';

SELECT 
    ag.name AS AvailabilityGroup,
    ar.replica_server_name AS ReplicaServer,
    ar.availability_mode_desc AS AvailabilityMode,
    ar.failover_mode_desc AS FailoverMode,
    ar.seeding_mode_desc AS SeedingMode,
    ars.role_desc AS CurrentRole,
    ars.connected_state_desc AS ConnectedState,
    ars.synchronization_health_desc AS SyncHealth,
    ars.last_connect_error_number AS LastConnectError,
    ars.last_connect_error_description AS LastConnectErrorDesc
FROM sys.availability_groups ag
JOIN sys.availability_replicas ar ON ag.group_id = ar.group_id
LEFT JOIN sys.dm_hadr_availability_replica_states ars ON ar.replica_id = ars.replica_id
WHERE ag.name IN (
    SELECT DISTINCT ag.name 
    FROM sys.availability_groups ag
    JOIN sys.availability_databases_cluster adc ON ag.group_id = adc.group_id
    WHERE adc.database_name IN (SELECT DatabaseName FROM @FailedDatabases)
)
ORDER BY ag.name, ar.replica_server_name;

PRINT '';
PRINT '';

-- =============================================
-- 3. Database Replica States (Detailed)
-- =============================================
PRINT '3. DATABASE REPLICA SYNCHRONIZATION DETAILS';
PRINT '----------------------------------------------------';

SELECT 
    ag.name AS AvailabilityGroup,
    ar.replica_server_name AS ReplicaServer,
    adc.database_name AS DatabaseName,
    drs.database_state_desc AS DatabaseState,
    drs.is_local AS IsLocal,
    drs.is_primary_replica AS IsPrimary,
    drs.synchronization_state_desc AS SyncState,
    drs.synchronization_health_desc AS SyncHealth,
    drs.is_suspended AS IsSuspended,
    drs.suspend_reason_desc AS SuspendReason,
    drs.last_hardened_lsn AS LastHardenedLSN,
    drs.last_redone_lsn AS LastRedoneLSN,
    drs.log_send_queue_size AS LogSendQueueKB,
    drs.redo_queue_size AS RedoQueueKB,
    drs.last_commit_time AS LastCommitTime
FROM sys.availability_groups ag
JOIN sys.availability_replicas ar ON ag.group_id = ar.group_id
JOIN sys.availability_databases_cluster adc ON ag.group_id = adc.group_id
LEFT JOIN sys.dm_hadr_database_replica_states drs 
    ON ar.replica_id = drs.replica_id 
    AND adc.database_name = DB_NAME(drs.database_id)
WHERE adc.database_name IN (SELECT DatabaseName FROM @FailedDatabases)
ORDER BY ag.name, adc.database_name, ar.replica_server_name;

PRINT '';
PRINT '';

-- =============================================
-- 4. Current Active Seeding Operations
-- =============================================
PRINT '4. ACTIVE SEEDING OPERATIONS (if any)';
PRINT '----------------------------------------------------';

IF EXISTS (SELECT 1 FROM sys.dm_hadr_physical_seeding_stats)
BEGIN
    SELECT 
        ag.name AS AvailabilityGroup,
        ar.replica_server_name AS LocalReplica,
        DB_NAME(pss.database_id) AS DatabaseName,
        pss.role_desc AS Role,
        pss.internal_state_desc AS InternalState,
        pss.transfer_rate_bytes_per_second / 1024.0 / 1024.0 AS TransferRateMBps,
        pss.transferred_size_bytes / 1024.0 / 1024.0 / 1024.0 AS TransferredGB,
        pss.database_size_bytes / 1024.0 / 1024.0 / 1024.0 AS DatabaseSizeGB,
        pss.start_time_utc AS StartTimeUTC,
        pss.estimate_time_complete_utc AS EstimatedCompleteUTC,
        pss.failure_code AS FailureCode,
        pss.failure_message AS FailureMessage
    FROM sys.dm_hadr_physical_seeding_stats pss
    JOIN sys.availability_groups ag ON pss.group_id = ag.group_id
    JOIN sys.availability_replicas ar ON pss.local_physical_replica_id = ar.replica_id
    ORDER BY ag.name, DB_NAME(pss.database_id);
END
ELSE
BEGIN
    PRINT 'No active seeding operations in progress.';
END

PRINT '';
PRINT '';

-- =============================================
-- 5. Check for Databases NOT in AG but should be
-- =============================================
PRINT '5. DATABASES NOT JOINED TO AG';
PRINT '----------------------------------------------------';

SELECT 
    fd.DatabaseName,
    CASE 
        WHEN d.database_id IS NULL THEN 'Database does not exist'
        WHEN hdrs.database_id IS NULL THEN 'Database exists but NOT in AG'
        ELSE 'In AG'
    END AS Status,
    d.state_desc AS CurrentState
FROM @FailedDatabases fd
LEFT JOIN sys.databases d ON fd.DatabaseName = d.name
LEFT JOIN sys.dm_hadr_database_replica_states hdrs ON d.database_id = hdrs.database_id
WHERE hdrs.database_id IS NULL OR d.database_id IS NULL
ORDER BY fd.DatabaseName;

PRINT '';
PRINT '';

-- =============================================
-- 6. Endpoint Status
-- =============================================
PRINT '6. HADR ENDPOINT STATUS';
PRINT '----------------------------------------------------';

SELECT 
    e.name AS EndpointName,
    e.type_desc AS EndpointType,
    e.state_desc AS EndpointState,
    e.port AS Port,
    sp.name AS ServiceAccount
FROM sys.database_mirroring_endpoints e
LEFT JOIN sys.server_principals sp ON e.principal_id = sp.principal_id
WHERE e.type_desc = 'DATABASE_MIRRORING';

PRINT '';
PRINT '';

-- =============================================
-- 7. Recommended Actions
-- =============================================
PRINT '7. RECOMMENDED ACTIONS';
PRINT '----------------------------------------------------';
PRINT '';
PRINT 'Based on the results above:';
PRINT '';
PRINT 'A. If DatabaseState = RESTORING:';
PRINT '   - Database is stuck in restoring state';
PRINT '   - Action: RESTORE DATABASE [DatabaseName] WITH RECOVERY;';
PRINT '   - Then re-join to AG';
PRINT '';
PRINT 'B. If SyncState = NOT SYNCHRONIZING and IsSuspended = 1:';
PRINT '   - Data movement is suspended';
PRINT '   - Action: ALTER DATABASE [DatabaseName] SET HADR RESUME;';
PRINT '';
PRINT 'C. If database NOT in AG:';
PRINT '   - Seeding failed and database was removed';
PRINT '   - Action: Re-add database to AG with manual seeding:';
PRINT '     1. On Primary: BACKUP DATABASE [DB] TO DISK = ''path''';
PRINT '     2. On Secondary: RESTORE DATABASE [DB] WITH NORECOVERY';
PRINT '     3. On Secondary: ALTER DATABASE [DB] SET HADR AVAILABILITY GROUP = [AGName]';
PRINT '';
PRINT 'D. If SyncHealth = NOT_HEALTHY:';
PRINT '   - Check network connectivity between replicas';
PRINT '   - Check endpoint status (should be STARTED)';
PRINT '   - Review error log for connection errors';
PRINT '';

PRINT '';
PRINT '========================================';
PRINT 'DIAGNOSTIC COMPLETE';
PRINT '========================================';
