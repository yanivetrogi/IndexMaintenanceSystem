# Test Scenarios of Index maintenance system

### Database discovery
- ✔ When there is a record in `ims_servers` and it has `discover_databases` and `schedule_id`, then the `ims_databases` should be populated with the records of discovered databases when the schedule occurs.
- ✔ When there is a record in `ims_servers` and it has `discover_databases` and `run_immediately`, then the `ims_databases` should be populated with the records of discovered databases right when the system starts.

### Index defragmentation
- ✔ should defragment
    - ✔ When there is a server and a database with `schedule_id`, then the defragmentation process should be started when the schedule occurs.
    - ✔ When there is a server and a database with `run_immediately`, then the defragmentation process should be started when the system starts.
- settings 
    - ✔ When the defragmentation settings in the `ims_databases` are not set, they should be inherited from the parent `ims_servers` record.
    - ⌛ When `exclude_last_partition` is set, then only N-1 partitions should be defragged.

### Scheduling
- ✔ description
    - ✔ when the schedule is inserted into `ims_schedules`, the `description` column of it should be automatically triggered.
- ✔ next_checks
    - ✔ when the schedule is assigned to the record of `ims_servers` with `discover_databases`, the record in `next_checks` is created.
    - ✔ when the schedule is assigned to the record of `ims_databases`, the record in `next_checks` is created.
    - ✔ when the schedule is assigned to the record of `ims_indexes`, the record in `next_checks` is created.
- ✔ the scheduling mechanism sql
- ✔ the description builder
- ✔ schedule inheritance

### Active column
- ✔ When a server record in `ims_servers` has `active=0`, all its databases should be excluded from maintenance operations regardless of their own active status.
- ✔ When a database record in `ims_databases` has `active=0`, all its indexes should be excluded from maintenance operations regardless of their own active status.
- ✔ When an index record in `ims_indexes` has `active=0`, it should be excluded from all maintenance operations.

### Run Immediately
- ✔ When a server has `run_immediately=1`
    - ✔ if `discover_indexes=1`, then the discovery process should start right away.
    - ✔ if `discover_databases=0`, then server's `run_immediately` is set to `0` and child databases' `run_immediately` having `null` are set to `1`
    - ✔ if `run_immediately` of child database is not null, then the database is not touched
    - ✔ if `run_immediately` of child database is null, than it's set to 1 to be processed during next cycle
- ✔ When a database has `run_immediately=1`, it should be taken into defragmentation process right away
- ✔ When a database has `run_immediately=1` and `schedule_id NOT NULL`, the two defragmentations should not interfere:
    - ✔ an immediate defragmentation should be initiated and a record in `ims_history_entries` should have `reason` = `run_immediately flag`;
    - ✔ a scheduled defragmentation should occur as expected and a record in `ims_history_entries` should have `reason` = `schedule [N] ...`;

### Index override
- ✔ when there is a record in `ims_indexes`, it's settings should be used over the settings of database and server.

### Index defragmentation
- ✔ When there is an error during defragmentation, it should be logged into history entries.

### MaxThreads
- ✔ different combinations for Server and DBs
    - ✔ 0 - unlimited threads
    - ✔ 1 - single thread
    - ✔ N - N parallel threads

### Graceful shutdown
- ✔ When the database is being processed by schedule and the active window is closing, the ongoing defragmentations should be completed, while non-started should not be started.