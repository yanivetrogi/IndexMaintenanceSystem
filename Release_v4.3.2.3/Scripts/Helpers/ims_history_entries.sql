SET NOCOUNT ON; SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; 
SELECT 
  [insert_time]
, [reason]
, [action]
, [error]
, [server]
, [database]
, [table]
, [index]
, [start_time]
, DATEDIFF(MINUTE, start_time, end_time) AS diff_mm
, CASE WHEN [size_kb_before] IS NULL OR [size_kb_before] <= 0 THEN 0 ELSE [size_kb_before] / 1024 END AS size_mb_before
, CASE WHEN [size_kb_after] IS NULL OR [size_kb_after] <= 0 THEN 0 ELSE [size_kb_after] / 1024 END AS size_mb_after
, [avg_fragmentation_percent_before] AS avg_frag_before
, [avg_fragmentation_percent_after] AS avg_frag_after
, [command]
FROM ims_history_entries 
WHERE 1 = 1
  AND insert_time > DATEADD(day, -7, GETDATE()) -- Filter for the last 7 days by default
  AND start_time IS NOT NULL
  --AND end_time IS NULL
  --AND action IN ('REORGANIZE', 'REBUILD')
  --AND server = ''
  --AND [index] = ''
  --AND [table] = ''
ORDER BY start_time DESC;
