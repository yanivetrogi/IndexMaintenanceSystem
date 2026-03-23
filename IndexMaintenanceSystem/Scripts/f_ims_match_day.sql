CREATE OR ALTER FUNCTION f_ims_match_day
(
    @startDate DATE,
    @freq_type INT,
    @freq_interval INT,
    @freq_relative_interval INT,
    @freq_recurrence_factor INT,
    @d DATE
)
RETURNS BIT
BEGIN
    DECLARE @diff INT;

    IF @freq_type = 1 -- one-time frequency
        RETURN CASE WHEN @d >= @startDate THEN 1 ELSE 0 END;
    ELSE IF @freq_type = 4 -- daily frequency
    BEGIN
        SET @diff = DATEDIFF(DAY, @startDate, @d);
        RETURN CASE WHEN (@diff >= 0 AND (@diff % @freq_interval = 0)) THEN 1 ELSE 0 END;
    END
    ELSE IF @freq_type = 8 -- weekly frequency
    BEGIN
        DECLARE @weekdayMask INT = POWER(2, DATEPART(WEEKDAY, @d) - 1);
        SET @diff = DATEDIFF(WEEK, @startDate, @d);
        RETURN CASE
            WHEN (@weekdayMask & @freq_interval) != 0
                AND (@freq_recurrence_factor = 0
                    OR @diff % @freq_recurrence_factor = 0)
            THEN 1
            ELSE 0
            END;
    END
    ELSE IF @freq_type = 16 -- monthly frequency
    BEGIN
        DECLARE @dayNum INT = DATEPART(DAY, @d);
        SET @diff = DATEDIFF(MONTH, @startDate, @d);
        RETURN CASE
            WHEN (@dayNum = @freq_interval)
                AND (@freq_recurrence_factor = 0
                    OR @diff % @freq_recurrence_factor = 0)
            THEN 1
            ELSE 0
        END;
    END
    ELSE IF @freq_type = 32 -- monthly-relative frequency
    BEGIN
        SET @diff = DATEDIFF(MONTH, @startDate, @d);
        IF @freq_recurrence_factor > 0 AND (@diff % @freq_recurrence_factor != 0)
            RETURN 0;

        IF @freq_relative_interval = 0
        BEGIN
            DECLARE @currentWeekday INT = DATEPART(WEEKDAY, @d);
            RETURN CASE
                WHEN @currentWeekday = @freq_interval
                -- freq_interval == 8 is not possible here
                    OR (@freq_interval = 9 AND @currentWeekday in (2, 3, 4, 5, 6))
                    OR (@freq_interval = 10 AND @currentWeekday in (1, 7))
                THEN 1
                ELSE 0
            END;
        END
        ELSE
        BEGIN
            RETURN CASE
                WHEN @d = [dbo].[f_ims_nth_relative_date_of_month](DATEPART(YEAR, @d), DATEPART(MONTH, @d), @freq_interval, @freq_relative_interval)
                THEN 1
                ELSE 0
            END;
        END
    END

    return 0;
END;