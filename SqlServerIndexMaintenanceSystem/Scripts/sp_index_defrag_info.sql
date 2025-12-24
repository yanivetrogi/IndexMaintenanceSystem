-- This script is being used by the SqlServerIndexMaintenanceSystem.
-- Don't change the interface of the stored procedure sp_index_defrag_info.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.sp_index_defrag_info') IS NULL
    EXEC ('CREATE PROCEDURE [dbo].[sp_index_defrag_info] as SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[sp_index_defrag_info]
(
     @object_id int
    ,@index_id int

    ,@schema_name sysname
    ,@table_name sysname
    ,@index_name sysname

    ,@partition_number int = 1
    ,@has_multiple_partitions bit = 0
    ,@reorg_threshold tinyint = 0
    ,@rebuild_threshold tinyint = 70
    ,@online bit = 0
    ,@maxdop tinyint = 0
    ,@sort_in_tempdb bit = 0

    ,@build_command bit = 1
)
AS
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN;

    DECLARE @online_partition_rebuild bit;

    -- SQL 2014 supports online partition rebuild
    IF (SELECT SERVERPROPERTY('ProductVersion') ) >= '12'
        SELECT @online_partition_rebuild = 1;
    ELSE
        SELECT @online_partition_rebuild = 0;

    -- Construct defrag command.
    DECLARE
         @blob bit
        ,@page_count bigint
        ,@avg_fragmentation_in_percent tinyint
        ,@command varchar(8000)
        ,@command_type varchar(10);

    -- Get blob status.
    SELECT @blob = CASE
        WHEN EXISTS (
            SELECT 1
            FROM sys.columns c
            WHERE c.object_id = @object_id
                AND (
                    system_type_id IN (35, 34, 241, 99)
                    /* column type that doesn't support on-line index rebuild (text,image,XML,ntext) */
                    OR max_length < 0
                )
        ) THEN CAST (1 AS BIT)
        ELSE CAST (0 AS BIT)
    END;

    SELECT 
        @page_count = s.page_count,
        @avg_fragmentation_in_percent = s.avg_fragmentation_in_percent
    FROM sys.dm_db_index_physical_stats(DB_ID(DB_NAME()), @object_id, @index_id, @partition_number, N'LIMITED') s;

    -- Construct the command based on the fragmentation
    IF @build_command = 1 AND @avg_fragmentation_in_percent >= @reorg_threshold
    BEGIN;
        -- Basic Syntax
        SELECT @command = N'ALTER INDEX [' + @index_name + N'] ON [' + @schema_name + N'].[' + @table_name + N']';

        IF
        (
            -- Should be reorganized due to the fragmentation percent and the thresholds
            (@avg_fragmentation_in_percent BETWEEN @reorg_threshold AND @rebuild_threshold )
                    OR
            -- Has clustered, blob and marked online, but it cant be rebuilt online due to the blob so we reorganize
            (@blob = 1 AND @index_id = 1 AND @online = 1 )
                    OR
            -- If the version is prior 2014 and table has multiple partitions it can only be reorganized online
            (@has_multiple_partitions = 1 AND @online = 1 AND @online_partition_rebuild = 0)
        )
        BEGIN
            -- Reorg
            SELECT @command = @command + N' REORGANIZE' , @command_type = N'REORGANIZE';
            IF @has_multiple_partitions = 1
                SELECT @command = @command + N' PARTITION = ' + CAST(@partition_number AS varchar(10));
        END
        ELSE
        BEGIN
            -- Rebuild
            SELECT @command = @command + N' REBUILD' , @command_type = N'REBUILD';
            IF @has_multiple_partitions = 1
                SELECT @command = @command + N' PARTITION = ' + CAST(@partition_number AS varchar(10));

            SELECT @command = @command + N' WITH (' + CASE WHEN @online = 1 THEN N'ONLINE = ON,' ELSE N'ONLINE = OFF,'
                        END + CASE WHEN @maxdop > 0 THEN N' MAXDOP = ' + CAST(@maxdop AS NCHAR(2)) + N',' ELSE N' MAXDOP = 0,' END
                        + CASE WHEN @sort_in_tempdb = 1 THEN N' SORT_IN_TEMPDB = ON' ELSE N' SORT_IN_TEMPDB = OFF' END + N');';
        END;

    END;

    -- Return the result.
    SELECT
        @command_type as [action],
        @page_count * 8 as [size_kb],
        @avg_fragmentation_in_percent as [avg_fragmentation_percent],
        @command as [command];

    RETURN;
END;
GO

EXEC sp_MS_marksystemobject 'dbo.sp_index_defrag_info';
GO
