-- Main procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_ims_get_schedule_next_execution_date_and_time] 
(
    @freq_type INT,                    -- 1=One-time, 4=Daily, 8=Weekly, 16=Monthly, 32=Monthly Relative
    @freq_interval INT,                -- Interval value (depends on freq_type)
    @freq_subday_type INT,             -- 0=Any time, 1=At specified time, 2=Seconds, 4=Minutes, 8=Hours
    @freq_subday_interval INT,         -- Subday interval value
    @freq_relative_interval INT,       -- For monthly relative: 1=First, 2=Second, 4=Third, 8=Fourth, 16=Last
    @freq_recurrence_factor INT,       -- Number of weeks/months between executions
    @active_start_date INT,            -- Start date (YYYYMMDD format)
    @active_end_date INT,              -- End date (YYYYMMDD format)
    @active_start_time INT,            -- Start time (HHMMSS format)
    @active_end_time INT,              -- End time (HHMMSS format)
    @last_execution_date INT,          -- Last execution date (YYYYMMDD format)
    @last_execution_time INT,          -- Last execution time (HHMMSS format)
    @next_execution_date INT OUTPUT,   -- Next execution date (YYYYMMDD format)
    @next_execution_time INT OUTPUT    -- Next execution time (HHMMSS format)
)
AS
BEGIN

/*
If there is no next rundate/time, the function returns NULL.
Function works in local system time.

freq_type - int

	How frequently a job runs for this schedule.

	1 = One time only
	4 = Daily
	8 = Weekly
	16 = Monthly
	32 = Monthly, relative to freq_interval
	(not supported)64 = Runs when the SQL Server Agent service starts
	(not supported)128 = Runs when the computer is idle

freq_interval - int	

	Days that the job is executed. Depends on the value of freq_type. 
	The default value is 0, which indicates that freq_interval is unused. 
	See the table below for the possible values and their effects.

freq_subday_type - int

	Units for the freq_subday_interval. The following are the possible 
	values and their descriptions.
	
	0 : Any time between the start time and end time
	1 : At the specified time (start time)
	2 : Seconds
	4 : Minutes
	8 : Hours

freq_subday_interval - int

	Number of freq_subday_type periods to occur between each execution of the job.

freq_relative_interval - int	

	When freq_interval occurs in each month, if freq_type is 32 (monthly relative). 
	Can be one of the following values:

	0 = freq_relative_interval is unused
	1 = First
	2 = Second
	4 = Third
	8 = Fourth
	16 = Last

freq_recurrence_factor - int

	Number of weeks or months between the scheduled execution of a job. 
	freq_recurrence_factor is used only if freq_type is 8, 16, or 32. 
	If this column contains 0, freq_recurrence_factor is unused.

active_start_date - int

	Date on which execution of a job can begin. The date is formatted as YYYYMMDD. NULL indicates today's date.

active_end_date - int

	Date on which execution of a job can stop. The date is formatted YYYYMMDD.

active_start_time - int

	Time on any day between active_start_date and active_end_date that job begins executing. 
	Time is formatted HHMMSS, using a 24-hour clock.

active_end_time - int

	Time on any day between active_start_date and active_end_date that job stops executing. 
	Time is formatted HHMMSS, using a 24-hour clock.

Value of freq_type				Effect on freq_interval
-------------------------------------------------------
	1 (once)					freq_interval is unused (0)
	4 (daily)					Every freq_interval days
	8 (weekly)					freq_interval is one or more of the following:
									1 = Sunday
									2 = Monday
									4 = Tuesday
									8 = Wednesday
									16 = Thursday
									32 = Friday
									64 = Saturday
	16 (monthly)				On the freq_interval day of the month
	32 (monthly, relative)		freq_interval is one of the following:
									1 = Sunday
									2 = Monday
									3 = Tuesday
									4 = Wednesday
									5 = Thursday
									6 = Friday
									7 = Saturday
									8 = Day
									9 = Weekday
									10 = Weekend day
	64 (starts when SQL Server Agent service starts)	freq_interval is unused (0)
	128 (runs when computer is idle)					freq_interval is unused (0)
*/
    SET NOCOUNT ON;

    -- Initialize output variables
    SET @next_execution_date = NULL;
    SET @next_execution_time = NULL;

    IF @active_end_time = 0
        SET @active_end_time = 235959;
    IF @active_start_date = 0
        SET @active_start_date = CONVERT(INT, FORMAT(CURRENT_TIMESTAMP, 'yyyyMMdd'));
    IF @active_end_date = 0
        SET @active_end_date = 99991231;

    -- 0 Parse and normalize schedule‐wide fields
    DECLARE @startDate DATE = ISNULL(TRY_CONVERT(DATE, CONVERT(VARCHAR(8), @active_start_date)), CONVERT(DATE, CURRENT_TIMESTAMP));
    DECLARE @endDate DATE = ISNULL(TRY_CONVERT(DATE, CONVERT(VARCHAR(8), @active_end_date)), CONVERT(DATE, '9999-12-31'));
    DECLARE @windowStart TIME = ISNULL(TRY_CONVERT(TIME, STUFF(STUFF(RIGHT('000000' + CONVERT(VARCHAR(6), @active_start_time), 6), 5, 0, ':'), 3, 0, ':')), CONVERT(TIME, '00:00:00'));
    DECLARE @windowEnd TIME = ISNULL(TRY_CONVERT(TIME, STUFF(STUFF(RIGHT('000000' + CONVERT(VARCHAR(6), @active_end_time), 6), 5, 0, ':'), 3, 0, ':')), CONVERT(TIME, '23:59:59'));
    DECLARE @isNightWindow BIT = CASE WHEN @windowStart > @windowEnd THEN 1 ELSE 0 END;

    -- 1 Validate schedule
    IF @active_start_date > 0 AND @active_end_date > 0 AND @active_start_date > @active_end_date
        RETURN;
    IF @freq_type NOT IN (1, 4, 8, 16, 32)
        RETURN;
    IF @freq_recurrence_factor < 0
        RETURN;
    IF @freq_type = 4 AND @freq_interval <= 0
        RETURN;
    IF @freq_type = 8 AND (@freq_interval <= 0 OR @freq_interval >= 128)
        RETURN;
    IF @freq_type = 16 AND (@freq_interval <= 0 OR @freq_interval >= 31)
        RETURN;
    IF @freq_type = 32
    BEGIN
        IF @freq_interval <= 0 OR @freq_interval >= 11
            RETURN;
        IF @freq_relative_interval NOT IN (0, 1, 2, 4, 8, 16)
            RETURN;
        IF @freq_interval = 8 AND @freq_relative_interval = 0
            RETURN;
    END;
    IF @freq_subday_type IN (2, 4, 8)
        IF @freq_subday_interval <= 0
            RETURN;

    -- 2 Reconstruct previous execution DateTime if both parts present
    DECLARE @lastExecutionDT DATETIME = NULL;
    IF @last_execution_date IS NOT NULL AND @last_execution_time IS NOT NULL
        SET @lastExecutionDT = [dbo].[ims_agent_datetime](@last_execution_date, @last_execution_time);

    -- 3 If schedule is one-time (freq_type == ONE_TIME_1) and it already ran, nothing more to do
    if @freq_type = 1 AND @lastExecutionDT IS NOT NULL
        RETURN;
    
    -- 4 Establish a baseline fromDT = currentDT
    DECLARE @baseFromDT DATETIME = CURRENT_TIMESTAMP;

    DECLARE @candidateFromDateTime DATETIME;

    -- 5 adjust the baseline if has already ran today
    IF @lastExecutionDT IS NOT NULL
    BEGIN
        -- 5.1 if schedule is any-time (freq_subday_type == ANY_TIME_0), nothing more to do for today - start from tomorrow
        if @freq_subday_type = 0
        BEGIN
            SET @candidateFromDateTime = CONVERT(DATETIME,CONVERT(TIME, DATEADD(DAY, 1, @lastExecutionDT)));
            IF @candidateFromDateTime > @baseFromDT
                SET @baseFromDT = @candidateFromDateTime;
        END
        ELSE
        BEGIN
            -- 5.2) If we have a previous execution, advance the baseline so that the next
            --    search starts strictly AFTER the previous run time. We add a small
            --    epsilon (1 second) which is sufficient because the core algorithm
            --    treats times >= fromDT. This prevents returning the same occurrence
            --    again (especially for freq_subday_type = AT_SPECIFIED_TIME_1) and
            --    allows continued intra-window repetition for repeating schedules.
            SET @candidateFromDateTime = DATEADD(SECOND, 1, @lastExecutionDT);
            IF @candidateFromDateTime > @baseFromDT
                SET @baseFromDT = @candidateFromDateTime;
        END;
    END
    ELSE
    BEGIN
        -- 6 (Optional) Clamp to active_start_date if schedule hasn't started yet
        -- Note: getNextOccurrence itself already enforces start/end logic,
        -- so this is defensive only.

        IF @baseFromDT < @startDate
            SET @baseFromDT = CONVERT(DATETIME, @startDate) + CONVERT(DATETIME, @windowStart);
    END;

    DECLARE @candidateT TIME;
    
    -- 7 if we might still be in yesterday’s rollover window
    IF @isNightWindow = 1 AND CONVERT(TIME, @baseFromDT) <= @windowEnd AND CONVERT(DATE, @baseFromDT) <= @endDate
    BEGIN
        DECLARE @prevDay DATE = DATEADD(DAY, -1, CONVERT(DATE, @baseFromDT));
        IF @prevDay >= @startDate and [dbo].[f_ims_match_day](@startDate, @freq_type, @freq_interval, @freq_relative_interval, @freq_recurrence_factor, @prevDay) = 1
        BEGIN
            SET @candidateT = [dbo].[f_ims_next_time_for_date](@windowStart, @windowEnd, @startDate, @freq_type, @freq_interval, @freq_subday_type, @freq_subday_interval, @freq_relative_interval, @freq_recurrence_factor, @prevDay, @baseFromDT);
            IF @candidateT IS NOT NULL
            BEGIN
                SET @next_execution_date = CONVERT(INT, FORMAT(@baseFromDT, 'yyyyMMdd'));
                SET @next_execution_time = CONVERT(INT, FORMAT(CONVERT(DATETIME, @candidateT), 'HHmmss'));
                RETURN;
            END;
        END;
    END;

    DECLARE @cursorD DATE = CASE WHEN CONVERT(DATE, @baseFromDT) < @startDate THEN @startDate ELSE CONVERT(DATE, @baseFromDT) END;

	PRINT 'StartDate ' + CONVERT(VARCHAR(MAX), @startDate);
	PRINT 'EndDate ' + CONVERT(VARCHAR(MAX), @endDate);
	PRINT '@active_end_date ' + (CONVERT(VARCHAR(MAX), @active_end_date));

    -- 8 scan from “today” (or later) forward
    WHILE @cursorD <= @endDate
    BEGIN
        PRINT 'Candidate ' + Convert(VARCHAR(MAX), @cursorD);

        IF [dbo].[f_ims_match_day](@startDate, @freq_type, @freq_interval, @freq_relative_interval, @freq_recurrence_factor, @cursorD) = 1
        BEGIN
            PRINT 'Match!';
			PRINT 'BaseFromDate: ' + CONVERT(VARCHAR(MAX),@baseFromDT);
            SET @candidateT = [dbo].[f_ims_next_time_for_date](@windowStart, @windowEnd, @startDate, @freq_type, @freq_interval, @freq_subday_type, @freq_subday_interval, @freq_relative_interval, @freq_recurrence_factor, @cursorD, @baseFromDT);

            IF @candidateT IS NOT NULL
            BEGIN
                PRINT FORMAT(CONVERT(DATETIME, @candidateT), 'HHmmss')
                SET @next_execution_date = CONVERT(INT, FORMAT(@cursorD, 'yyyyMMdd'));
                SET @next_execution_time = CONVERT(INT, FORMAT(CONVERT(DATETIME, @candidateT), 'HHmmss'));
                RETURN;
            END;
        END;

        SET @cursorD = DATEADD(DAY, 1, @cursorD);
    END

    -- 9 if scan doesn't give result, the null is returned
END;