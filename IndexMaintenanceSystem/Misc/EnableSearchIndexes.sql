-- Enable search_indexes to auto-discover fragmented indexes
-- Run these against the IndexMaintenanceSystem database

-- ============================================
-- OPTION 1: Enable at SERVER level (RECOMMENDED)
-- ============================================
-- This enables auto-discovery for ALL databases on server YOUR_SERVER_NAME
-- Any database with search_indexes = NULL will inherit this setting

UPDATE ims_servers 
SET search_indexes = 1 
WHERE server_id = 1;  -- YOUR_SERVER_NAME

-- Verify the change
SELECT server_id, name, search_indexes, discover_databases, schedule_id
FROM ims_servers
WHERE server_id = 1;


-- ============================================
-- OPTION 2: Enable at DATABASE level (Specific)
-- ============================================
-- This enables auto-discovery ONLY for YOUR_DATABASE_NAME database
-- Overrides the server-level setting for this specific database

UPDATE ims_databases 
SET search_indexes = 1 
WHERE database_id = 1;  -- YOUR_DATABASE_NAME

-- Verify the change
SELECT database_id, server_id, name, search_indexes, schedule_id
FROM ims_databases
WHERE database_id = 1;


-- ============================================
-- OPTION 3: Enable for ALL databases on the server
-- ============================================
-- This sets search_indexes = 1 for every database on YOUR_SERVER_NAME

UPDATE ims_databases 
SET search_indexes = 1 
WHERE server_id = 1;

-- Verify the changes
SELECT database_id, server_id, name, search_indexes, schedule_id
FROM ims_databases
WHERE server_id = 1
ORDER BY database_id;


-- ============================================
-- RECOMMENDED APPROACH
-- ============================================
-- Use OPTION 1 (server level) so all databases benefit from auto-discovery
-- Unless you want to control it per-database, then use OPTION 2 or 3
