CREATE FUNCTION f_ims_next_time_for_date
(
    @windowStart TIME,
    @windowEnd TIME,
    @startDate DATE,
    @freq_type INT,
    @freq_interval INT,
    @freq_subday_type INT,
    @freq_subday_interval INT,
    @freq_relative_interval INT,
    @freq_recurrence_factor INT,
    @d DATE,
    @fromDT DATETIME
)
RETURNS TIME
BEGIN
-- This function is designed to return such a time 't', that in pair with 'd' is after fromDT.
-- In other words fromDT < CONVERT(DATETIME, d) + CONVERT(DATETIME, t)

    DECLARE @isNightWindow BIT = CASE WHEN @windowStart > @windowEnd THEN 1 ELSE 0 END;
    DECLARE @fromD DATE = CONVERT(DATE, @fromDT);
    DECLARE @fromT TIME = CONVERT(TIME, @fromDT);

    -- 1 Handle non-repeating window (ANY_TIME_0) as a single firing at windowStart or fromDT.time
    IF @freq_subday_type = 0
    BEGIN
        IF @fromD < @d OR (@fromD = @d AND @fromT <= @windowStart)
            RETURN @windowStart;
        ELSE IF @fromD = @d AND
            ((@isNightWindow = 1 AND (@fromT >= @windowStart OR @fromT <= @windowEnd))
                OR (@isNightWindow = 0 AND @fromT >= @windowStart AND @fromT <= @windowEnd))
            RETURN @fromT;
        ELSE
            RETURN NULL;
    END;

    -- 2 Handle non-repeating windows (AT_SPECIFIED_TIME_1) as a single firing at windowStart
    IF @freq_subday_type = 1 -- = At specific time
    BEGIN
        IF @fromD < @d OR (@fromD = @d AND @fromT <= @windowStart)
            RETURN @windowStart;
        ELSE
            RETURN NULL;
    END;

    -- 3 Compute the repeat interval in seconds for true repeating types (seconds/minutes/hours)
    DECLARE @unitSec INT = CASE
        WHEN @freq_subday_type = 2 THEN 1
        WHEN @freq_subday_type = 4 THEN 60
        WHEN @freq_subday_type = 8 THEN 3600
        ELSE 1
    END;

    DECLARE @stepSec INT = @unitSec * @freq_subday_interval;

    -- 4 If this is the rollover day of a night-window, try that first
    IF @isNightWindow = 1
    BEGIN
        DECLARE @prevDay DATE = DATEADD(DAY, -1, @d);
        if [dbo].[f_ims_match_day](@startDate, @freq_type, @freq_interval, @freq_relative_interval, @freq_recurrence_factor, @prevDay) = 1
        BEGIN
            DECLARE @cursorDateTime DATETIME = CONVERT(DATETIME, @prevDay) + CONVERT(DATETIME, @windowStart);
            DECLARE @cursorLatestDateTime DATETIME = CONVERT(DATETIME, @d) + CONVERT(DATETIME, @windowEnd);
            DECLARE @cursorMinTargetDateTime DATETIME = CASE WHEN @fromDT > CONVERT(DATETIME, @d) THEN @fromDT ELSE CONVERT(DATETIME, @d) END;
            DECLARE @cursorDiffSeconds INT = CEILING(DATEDIFF(SECOND, @cursorDateTime, @cursorMinTargetDateTime) * 1.0 / @stepSec) * @stepSec;

            SET @cursorDateTime = DATEADD(SECOND, @cursorDiffSeconds, @cursorDateTime);

            IF (@cursorDateTime <= @cursorLatestDateTime)
                RETURN CONVERT(TIME, @cursorDateTime);
        END;
    END;

    -- 5 Now handle the “main” segment on day = d
    --   if night-window, it runs from windowStart → 23:59:59; else windowStart → windowEnd

    DECLARE @segStartTime TIME = @windowStart;
    DECLARE @segEndTime TIME = CASE WHEN @isNightWindow = 1 THEN CONVERT(TIME, '23:59:59') ELSE @windowEnd END;

    DECLARE @targetStartDateTime DATETIME = CONVERT(DATETIME, @d) + CONVERT(DATETIME, @windowStart);
    DECLARE @minTargetDateTime DATETIME = CASE WHEN @fromDT > @targetStartDateTime THEN @fromDT ELSE @targetStartDateTime END;
    DECLARE @targetLatestDateTime DATETIME = CONVERT(DATETIME, @d) + CONVERT(DATETIME, @segEndTime);

    IF @minTargetDateTime <= @targetLatestDateTime
    BEGIN
        DECLARE @diffSeconds INT = CEILING(DATEDIFF(SECOND, @targetStartDateTime, @minTargetDateTime) * 1.0 / @stepSec) * @stepSec;
        DECLARE @candDateTime DATETIME = DATEADD(SECOND, @diffSeconds, @targetStartDateTime);

        IF @candDateTime <= @targetLatestDateTime
            RETURN CONVERT(TIME, @candDateTime);
    END

    -- 6 no more runs on this date
    RETURN NULL;
END;