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

When both `run_immediately` and `schedule` are happen to mach the time of their `next_execution`, the system will process them sequentially in random order. Meaning, the task might be executed twice: one right after the other. That is an expected behavior. So keep in mind existing schedules when setting `run_immediately` flag on.

------

## Infrastructure

#### Triggers

Each entity's trigger reacts to changing `run_immediately` in the same way as it reacts to `schedule_id`:
The record in `next_checks` is created for each required entity with `schedule_id` being NULL.
The server gets a record with an empty schedule if `discover_databases` is set.
The database gets a record with an empty schedule if `discover_indexes` is set.
The index gets a record to be checked and defragged.

After the respective action is performed, the `previous_execution_date` and `previous_execution_time` fields of the `next_check` are updated with the current date and time.
The following calls of `plan_next_check` targeting these records set `next_execution_date` and `next_execution_time` to null to mark that the execution was performed successfully.

There is an `on update` trigger on `next_checks` that checks for the records without a schedule being marked as done (`next_execution_date/time` set to NULL).
It goes up the tree to find the associated `run_immediately` flag and then checks if all the children have completed the execution. When the condition is met, the root of this subtree is unflagged, causing all the completed `next_checks` to be disposed.