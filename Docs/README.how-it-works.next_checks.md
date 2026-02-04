# Next Checks management

The records in `ims_next_checks` are managed with the help of triggers on `ims_servers`, `ims_databases`, and `ims_indexes` tables.

Triggers' code can be found in [Migrations folder](./IndexMaintenanceSystem/Migrations/)

## Pseudocodes of triggers

#### [Servers](./IndexMaintenanceSystem/Migrations/_02_ServersTrigger.cs)

```
if active
    if schedule or run_immediately
        if discover_databases
            delete next_check for the server of different schedules (in case, if the schedule has been changed)
            if run_immediately
                set next_check for the server without schedule
            if schedule
                set next_check for the server with schedule
        else
            delete next_check for the server

        for children without their own schedule/run_immediately
            delete next_checks of different schedules (in case, if the schedule has been changed)
            if schedule
                set next_checks with schedule
            if run_immediately
                set next_checks without schedule
    else
        delete next_check for the server and the children, who derives server's schedule
    for children with their own schedule
        set next_checks
else
    delete next_checks for the complete tree
        delete next_check for the server
        delete next_checks for all the children (including the ones with their own schedules)
```

#### [Databases](./IndexMaintenanceSystem/Migrations/_03_DatabasesTrigger.cs)

```
skip dbs, whose parent server isn't active
    because everything was managed during server inactivation.
schedule = db.schedule ?? server.schedule
run_immediately = db.run_immediately || (db.schedule=null AND server.run_immediately)
if active
    if schedule or run_immediately
        if index_discovery
            delete next_check for the database of different schedules (in case, if the schedule has been changed)
            if run_immediately
                set next_check for the database without schedule
            if schedule
                set next_check for the database with schedule
        else
            delete next_check for the database
        for child indexes without their own schedule/run_immediately
            delete next_checks of different schedules (in case, if the schedule has been changed)
            if run_immediately
                set next_checks without schedule
            if schedule
                set next_checks with schedule
    else
        delete next_checks for the database and for children, who derives server's schedule
    for child indexes with their own schedule
        set next_checks
else
    delete next_checks for the complete subtree (including the ones with own schedules)

```

#### [Indexes](./IndexMaintenanceSystem/Migrations/_04_IndexesTrigger.cs)

```
skip indexes, whose parents (server or db) are not active
    because everything was managed during parent inactivation.
schedule = index.schedule ?? db.schedule ?? server.schedule
run_immediately = index.run_immediately OR (index.schedule=null AND db.run_immediately) OR (index.schedule=null AND db.schedule=null AND server.run_immediately)
if active
    delete next_check for the indexes of different schedules (in case, if the schedule or run_immediately has been changed)
    if run_immediately
        set next_check for the index without schedule
    if schedule
        set next_check for the index with schedule
else
    delete next_check for the index
```

