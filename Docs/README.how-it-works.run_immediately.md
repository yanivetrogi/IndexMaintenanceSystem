# Run Immediately

This functionality is designed mostly for testing purposes.

Each `ims` entity has a flag `run_immediately`. It's disabled by default.
Setting it to true will make the system run the associated activities right away, skipping the schedule.
Generally, it can be imagined as a shortcut to `run one time any time as soon as possible`.
Any child entity without its own schedule derives this setting from the parent.
The setting has lower priority than the `active` flag.

Server:

- When `database_discovery` is enabled, it's enqueued right away, skipping the schedule if any.
- All child databases/indexes without their own schedules are enqueued right away.

Database:

- When `index_discovery` is enabled, it's enqueued right away, skipping the schedule if any.
- All child indexes without their own schedules are enqueued right away.

After the run is complete for all the subtree, the flag is reverted automatically.

---

## Important

When both `run_immediately` and `schedule` happen to match the time of their `next_execution`, the system will process them sequentially in random order. Meaning, the task might be executed twice: one right after the other. That is an expected behavior. So keep in mind existing schedules when setting `run_immediately` flag on.

------

## Infrastructure

#### Triggers

Triggers on the `ims_servers`, `ims_databases`, and `ims_indexes` tables react to changing the `run_immediately` flag by creating or updating records in the `ims_next_checks` table with a `NULL` `schedule_id`.

The `run_immediately` flag is automatically reverted (set back to `0` or `NULL`) by the application services (`DatabaseDefragger`, `ServerProcessor`, etc.) once the associated tasks have completed successfully.
