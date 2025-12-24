CREATE FUNCTION f_ims_nth_relative_date_of_month
(
    @year INT,
    @month INT,
    @freq_interval INT,
    @freq_relative_interval INT
)
RETURNS DATE
BEGIN
    DECLARE @offset INT;
    DECLARE @count INT = 0;
    DECLARE @day INT = 1;
    DECLARE @dow INT;
    DECLARE @d DATE;

    IF @freq_relative_interval = 0
        RETURN NULL;

    -- 1. Decode ordinalIndex from the bit-flag:
    --    1->1, 2->2, 4->3, 8->4, 16->5
    DECLARE @ordinalIndex INT = LOG(@freq_relative_interval, 2) + 1

    DECLARE @totalDays INT = DATEPART(DAY, EOMONTH(DATEFROMPARTS(@year, @month, 1)))

    IF @freq_interval in (1, 2, 3, 4, 5, 6, 7)
    -- “Nth [Sun…Sat] of the month”
    BEGIN
        DECLARE @desiredDow INT = @freq_interval;
        DECLARE @firstDay DATE = DATEFROMPARTS(@year, @month, 1);
        DECLARE @firstDow INT = DATEPART(WEEKDAY, @firstDay);

        -- offset to first desiredDow in the month
        SET @offset = (@desiredDow - @firstDow + 7) % 7;
        DECLARE @firstOccur DATE = DATEADD(DAY, @offset, @firstDay);

        IF @ordinalIndex < 5
        BEGIN
            DECLARE @candidate DATE = DATEADD(DAY, (@ordinalIndex - 1) * 7, @firstOccur);
            RETURN CASE WHEN DATEPART(MONTH, @candidate) = @month THEN @candidate ELSE NULL END;
        END
        ELSE
        BEGIN
            DECLARE @lastDay DATE = DATEFROMPARTS(@year, @month, @totalDays);
            DECLARE @lastDow INT = DATEPART(WEEKDAY, @lastDay);
            DECLARE @backOff INT = (@lastDow - @desiredDow + 7) % 7;
            RETURN DATEADD(DAY, -@backOff, @lastDay)
        END;
    END;

    IF @freq_interval = 8
    -- “Nth day of the month” (literal day‐of‐month)1
    BEGIN
        IF @ordinalIndex < 5
        BEGIN
            -- 1st → 1, 2nd → 2, 3rd → 3, 4th → 4
            IF @ordinalIndex <= @totalDays
                RETURN DATEFROMPARTS(@year, @month, @ordinalIndex);
            ELSE
                RETURN NULL;
        END
        ELSE
        BEGIN
            -- “Last” → last calendar day
            RETURN DATEFROMPARTS(@year, @month, @totalDays);
        END;
    END;

    IF @freq_interval = 9
    -- “Nth Weekday of month” (Mon–Fri)
    BEGIN
        IF @ordinalIndex < 5
        BEGIN
            SET @day = 1;
            
            WHILE @day <= @totalDays
            BEGIN
                SET @dow = DATEPART(WEEKDAY, DATEFROMPARTS(@year, @month, @day));
                IF @dow >= 2 AND @dow <= 6 -- Mon=2..Fri=6
                BEGIN
                    SET @count = @count + 1;
                    IF @count = @ordinalIndex
                        RETURN DATEFROMPARTS(@year, @month, @day);
                END;
                SET @day = @day + 1;
            END;
        END
        ELSE -- last weekday -> scan backward
        BEGIN
            SET @offset = 0;

            WHILE @offset < @totalDays
            BEGIN
                SET @d = DATEFROMPARTS(@year, @month, @totalDays - @offset);
                SET @dow = DATEPART(WEEKDAY, @d);
                IF @dow >= 2 AND @dow <= 6
                    RETURN @d

                SET @offset = @offset + 1;
            END;
        END;

        RETURN NULL;
    END;

    IF @freq_interval = 10
    -- “Nth Weekend-day of month” (Sat, Sun)
    BEGIN

        if @ordinalIndex < 5
        BEGIN
            SET @day = 1;
            SET @count = 0;
            
            WHILE @day <= @totalDays
            BEGIN
                SET @dow = DATEPART(WEEKDAY, DATEFROMPARTS(@year, @month, @day));

                IF @dow IN (1, 7) -- Sun=1 or Sat=7
                BEGIN
                    SET @count = @count + 1;
                    IF @count = @ordinalIndex
                        RETURN DATEFROMPARTS(@year, @month, @day);
                END;

                SET @day = @day + 1;
            END;
        END
        ELSE -- last weekend-day -> scan backward
        BEGIN
            SET @offset = 0;
            WHILE @offset < @totalDays
            BEGIN
                SET @d = DATEFROMPARTS(@year, @month, @totalDays - @offset);
                SET @dow = DATEPART(WEEKDAY, @d);
                IF @dow IN (1, 7)
                    RETURN @d;

                SET @offset = @offset + 1;
            END;
        END;

        RETURN NULL;
    END;
    
    -- default
    RETURN NULL;
END;