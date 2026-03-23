-- Insert test schedules for each frequency type and combination
DELETE from dbo.ims_schedules;

DECLARE
	@CurrentDateInt INT = CONVERT(INT, FORMAT(CURRENT_TIMESTAMP, 'yyyyMMdd')),
	@FutureFutureDateInt INT = CONVERT(INT, FORMAT(DATEADD(MONTH, 6, CURRENT_TIMESTAMP), 'yyyyMMdd')),
	@FutureDateInt INT = CONVERT(INT, FORMAT(DATEADD(MONTH, 3, CURRENT_TIMESTAMP), 'yyyyMMdd')),
	@PastDateInt INT = CONVERT(INT, FORMAT(DATEADD(MONTH, -3, CURRENT_TIMESTAMP), 'yyyyMMdd')),
	@PastPastDateInt INT = CONVERT(INT, FORMAT(DATEADD(MONTH, -6, CURRENT_TIMESTAMP), 'yyyyMMdd')),
	@FarFutureDateInt INT = CONVERT(INT, FORMAT(DATEADD(YEAR, 6, CURRENT_TIMESTAMP), 'yyyyMMdd')),
	@CurrentTimeInt INT = CONVERT(INT, FORMAT(CURRENT_TIMESTAMP, 'HHmmss')),
	@FutureFutureTimeInt INT = CASE 
		WHEN CONVERT(DATE, DATEADD(HOUR, 5, CURRENT_TIMESTAMP)) > CONVERT(DATE, CURRENT_TIMESTAMP) 
		THEN 235959 
		ELSE CONVERT(INT, FORMAT(DATEADD(HOUR, 5, CURRENT_TIMESTAMP), 'HHmmss')) 
	END,
	@FutureTimeInt INT = CASE 
		WHEN CONVERT(DATE, DATEADD(MINUTE, 5, CURRENT_TIMESTAMP)) > CONVERT(DATE, CURRENT_TIMESTAMP) 
		THEN 235959 
		ELSE CONVERT(INT, FORMAT(DATEADD(MINUTE, 5, CURRENT_TIMESTAMP), 'HHmmss')) 
	END,
	@PastTimeInt INT = CASE
		WHEN CONVERT(DATE, DATEADD(MINUTE, -5, CURRENT_TIMESTAMP)) < CONVERT(DATE, CURRENT_TIMESTAMP)
		THEN 0
		ELSE CONVERT(INT, FORMAT(DATEADD(MINUTE, -5, CURRENT_TIMESTAMP), 'HHmmss'))
	END,
	@PastPastTimeInt INT = CASE
		WHEN CONVERT(DATE, DATEADD(MINUTE, -10, CURRENT_TIMESTAMP)) < CONVERT(DATE, CURRENT_TIMESTAMP)
		THEN 0
		ELSE CONVERT(INT, FORMAT(DATEADD(MINUTE, -10, CURRENT_TIMESTAMP), 'HHmmss'))
	END;


INSERT INTO dbo.ims_schedules (
    name, freq_type, freq_interval, freq_subday_type, freq_subday_interval, 
    freq_relative_interval, freq_recurrence_factor, active_start_date, active_end_date, 
    active_start_time, active_end_time
)
VALUES

-- OneTime schedules
-- Template:
-- ('OneTime - Anytime', 1, 0, 0, 0, 0, 0, 0, 0, 0, 0),
-- Tests
('OneTime - Starts and finishes today, timeframe unset - Expected at current date and time', 1, 0, 0, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),
('OneTime - Starts and finishes today, at 00:00 - Expected Never', 1, 0, 1, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),
('OneTime - Starts today, active_time in past - Expected tomorrow at active_start_time', 1, 0, 1, 0, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('OneTime - Starts today, active_time in future - Expected today at active_start_time', 1, 0, 1, 0, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('OneTime - Starts in past - Expected at current date and time', 1, 0, 0, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('OneTime - Starts in past, at 00:00 - Expected tomorrow at 00:00', 1, 0, 1, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('OneTime - Starts in past, active_time in past - Expected tomorrow at active_start_time', 1, 0, 1, 0, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('OneTime - Starts in past, active_time in future - Expected today at active_start_time', 1, 0, 1, 0, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('OneTime - Starts in past, active_time range now - Expected today at current time', 1, 0, 0, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),
('OneTime - Starts in past, active_time range now - Expected tomorrow at active_start_time', 1, 0, 1, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('OneTime - Starts in future - Expected in future', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, 0, 0, 0),
('OneTime - Starts in future, active_time in past - Expected in future at active_start_time', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('OneTime - Starts in future, active_time in future - Expected in future at active_start_time', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('OneTime - Starts in future, active_time now - Expected in future at active_start_time', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('OneTime - Starts in past and ends in past - Expected never', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('OneTime - Starts in past and ends in past, active_time in past - Expected never', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('OneTime - Starts in past and ends in past, active_time now - Expected never', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('OneTime - Starts in past and ends in past, active_time in future - Expected never', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('OneTime - Starts in past and ends in future, at 0:00 - Expected tomorrow at 0:00', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('OneTime - Starts in past and ends in future - Expected at current date and time', 1, 0, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('OneTime - Starts in past and ends in future, active_time in past - Expected tomorrow at active_start_time', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('OneTime - Starts in past and ends in future, active_time in future - Expected today at active_start_time', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('OneTime - Starts in past and ends in future, active_time now, at active_start_time  - tomorrow at active_start_time', 1, 0, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),
('OneTime - Starts in past and ends in future, active_time now - Expected today at current time', 1, 0, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('OneTime - Starts in future and ends in future - Expected in future', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('OneTime - Starts in future and ends in future, active_time in past - Expected in future at active_start_time', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('OneTime - Starts in future and ends in future, active_time in future - Expected in future at active_start_time', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('OneTime - Starts in future and ends in future, active_time now - Expected in future at active_start_time', 1, 0, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),


-- Daily schedules
-- Template:
-- ('Every 3 days - Anytime', 4, 3, 0, 0, 0, 0, 0, 0, 0, 0),
-- Tests
('Daily - Anytime - Expected at current date and time OR (IF last_execution_time today) tomorrow at 0:00', 4, 1, 0, 0, 0, 0, 0, 0, 0, 0),

('Daily - at 0:00 - Expected tomorrow at 0:00', 4, 1, 1, 0, 0, 0, 0, 0, 0, 0),
('Daily - Starts today, finishes today - Expected at current date and time OR (IF last_execution_time today) Never', 4, 1, 0, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),
('Daily - Starts today, finishes today, at 0:00 - Expected Never', 4, 1, 1, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Daily - Starts today, active_time in past - Expected tomorrow at active_start_time', 4, 1, 1, 0, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Daily - Starts today, active_time in future - Expected today at active_start_time', 4, 1, 1, 0, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('Daily - Starts in past - Expected tomorrow at 0:00', 4, 1, 1, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('Daily - Starts in past - Expected at current date and time', 4, 1, 0, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('Daily - Starts in past, active_time in past - Expected tomorrow at active_start_time', 4, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Daily - Starts in past, active_time in future - Expected today at active_start_time', 4, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Daily - Starts in past, active_time now, at active_start_time - Expected tomorrow at active_start_time', 4, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),
('Daily - Starts in past, active_time now - Expected today at current time', 4, 1, 0, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Daily - Starts in future, at 0:00 - Expected in future', 4, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Daily - Starts in future - Expected in future', 4, 1, 0, 0, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Daily - Starts in future, active_time in past, at active_start_time - Expected in future at active_start_time', 4, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Daily - Starts in future, active_time in future, at active_start_time - Expected in future at active_start_time', 4, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Daily - Starts in future, active_time now, at active_start_time - Expected in future at active_start_time', 4, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Daily - Starts in past and ends in past - Expected never', 4, 1, 0, 0, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Daily - Starts in past and ends in past, at active_start_time - Expected never', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Daily - Starts in past and ends in past, active_time in past, at active_start_time - Expected never', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Daily - Starts in past and ends in past, active_time now, at active_start_time - Expected never', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Daily - Starts in past and ends in past, active_time in future, at active_start_time - Expected never', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Daily - Starts in past and ends in future, at 0:00 - Expected tomorrow at 0:00', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Daily - Starts in past and ends in future - Expected at current date and time', 4, 1, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Daily - Starts in past and ends in future, active_time in past, at active_start_time - Expected tomorrow at active_start_time', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Daily - Starts in past and ends in future, active_time in future, at active_start_time - Expected today at active_start_time', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Daily - Starts in past and ends in future, active_time now, at active_start_time - Expected tomorrow at active_start_time', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),
('Daily - Starts in past and ends in future, active_time now - Expected today at current time', 4, 1, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('Daily - Starts in future and ends in future, at 0:00 - Expected in future', 4, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Daily - Starts in future and ends in future - Expected in future', 4, 1, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Daily - Starts in future and ends in future, active_time in past - Expected in future at active_start_time', 4, 1, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Daily - Starts in future and ends in future, active_time in future - Expected in future at active_start_time', 4, 1, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Daily - Starts in future and ends in future, active_time now - Expected in future at active_start_time', 4, 1, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),


('Daily - every day at 3:00:00 PM - Expected today/tomorrow at 3 pm', 4, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 150000, 235959),
('Daily - every day - Expected today at current time', 4, 1, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),

('Daily Hourly - every 3 day(s) every 4 hour(s) between 3:00:00 AM and 6:00:00 PM - Expected today at 3am-6pm or in 3 days at 3 am', 4, 3, 8, 4, 0, 0,
	@CurrentDateInt, @FutureDateInt, 30000, 180000),
('Daily Hourly - every 3 day(s) every 4 hour(s) between 3:00:00 AM and 6:00:00 PM - Expected in 3 days at 3 am', 4, 3, 8, 4, 0, 0,
	@CurrentDateInt, @FutureDateInt, 30000, 180000),


('Daily Nightly - every night every 2 hours between 10pm and 3pm', 4, 1, 8, 2, 0, 0,
	@PastDateInt, @FutureDateInt, 220000, 150000),


-- Subday schedules
-- Template:
-- ('Every 5 days - Every 3 hours within the matching day',    4, 5, 8, 3,  0, 0, 0, 0, 0, 0),
-- ('Every 6 days - Every 25 minutes within the matching day', 4, 6, 4, 25, 0, 0, 0, 0, 0, 0),
-- Tests
('Subday Hourly - Every hour - Expected at current date at next hour', 4, 1, 8, 1, 0, 0,
	0, 0, 0, 0),

('Subday Hourly - Starts today, finishes today - Expected at current date at next hour', 4, 1, 8, 1, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Subday Hourly - Starts today, active_time in past - Expected tomorrow at active_start_time', 4, 1, 8, 1, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Subday Hourly - Starts today, active_time in future - Expected today at active_start_time', 4, 1, 8, 1, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Hourly - Starts today, active_time now - Expected today at current time', 4, 1, 8, 1, 0, 0,
	@CurrentDateInt, 0, @PastTimeInt, @FutureFutureTimeInt),

('Subday Hourly - Starts in past - Expected at current date and at next hour', 4, 1, 8, 1, 0, 0,
	@PastDateInt, 0, 0, 0),
('Subday Hourly - Starts in past, active_time in past - Expected tomorrow at active_start_time', 4, 1, 8, 1, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Subday Hourly - Starts in past, active_time in future - Expected today at active_start_time', 4, 1, 8, 1, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Hourly - Starts in past, active_time now - Expected at current date at next hour', 4, 1, 8, 1, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Subday Hourly - Starts in future - Expected in future at 0:00', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Subday Hourly - Starts in future, active_time in past - Expected in future at active_start_time', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Subday Hourly - Starts in future, active_time in future - Expected in future at active_start_time', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Hourly - Starts in future, active_time now - Expected in future at active_start_time', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Subday Hourly - Starts in past and ends in past - Expected never', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Subday Hourly - Starts in past and ends in past, active_time in past - Expected never', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Subday Hourly - Starts in past and ends in past, active_time now - Expected never', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Subday Hourly - Starts in past and ends in past, active_time in future - Expected never', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Subday Hourly - Starts in past and ends in future - Expected at current date and at next hour', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Subday Hourly - Starts in past and ends in future, active_time in past - Expected tomorrow at active_start_time', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Subday Hourly - Starts in past and ends in future, active_time in future - Expected today at active_start_time', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Hourly - Starts in past and ends in future, active_time now - Expected today at next hour', 4, 1, 8, 1, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureFutureTimeInt),

('Subday Hourly - Starts in future and ends in future - Expected in future', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Subday Hourly - Starts in future and ends in future, active_time in past - Expected in future at active_start_time', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Subday Hourly - Starts in future and ends in future, active_time in future - Expected in future at active_start_time', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Hourly - Starts in future and ends in future, active_time now - Expected in future at active_start_time', 4, 1, 8, 1, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),

('Subday Every 30 Minutes - Starts today, finishes never - Expected at current date and time', 4, 1, 4, 30, 0, 0, 0, 0, 0, 0),
('Subday Every 30 Minutes - Starts today, finishes today - Expected at current date and time', 4, 1, 4, 30, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Subday Every 30 Minutes - Starts today, active_time in past - Expected tomorrow at active_start_time', 4, 1, 4, 30, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Subday Every 30 Minutes - Starts today, active_time in future - Expected today at active_start_time', 4, 1, 4, 30, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('Subday Every 30 Minutes - Starts in past - Expected at current date and time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, 0, 0, 0),
('Subday Every 30 Minutes - Starts in past, active_time in past - Expected tomorrow at active_start_time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Subday Every 30 Minutes - Starts in past, active_time in future - Expected today at active_start_time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Every 30 Minutes - Starts in past, active_time now - Expected today at current time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Subday Every 30 Minutes - Starts in future - Expected in future', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Subday Every 30 Minutes - Starts in future, active_time in past - Expected in future at active_start_time', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Subday Every 30 Minutes - Starts in future, active_time in future - Expected in future at active_start_time', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Every 30 Minutes - Starts in future, active_time now - Expected in future at active_start_time', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Subday Every 30 Minutes - Starts in past and ends in past - Expected never', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Subday Every 30 Minutes - Starts in past and ends in past, active_time in past - Expected never', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Subday Every 30 Minutes - Starts in past and ends in past, active_time now - Expected never', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Subday Every 30 Minutes - Starts in past and ends in past, active_time in future - Expected never', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Subday Every 30 Minutes - Starts in past and ends in future - Expected at current date and time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Subday Every 30 Minutes - Starts in past and ends in future, active_time in past - Expected tomorrow at active_start_time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Subday Every 30 Minutes - Starts in past and ends in future, active_time in future - Expected today at active_start_time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Every 30 Minutes - Starts in past and ends in future, active_time now - Expected today at current time', 4, 1, 4, 30, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('Subday Every 30 Minutes - Starts in future and ends in future - Expected in future', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Subday Every 30 Minutes - Starts in future and ends in future, active_time in past - Expected in future at active_start_time', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Subday Every 30 Minutes - Starts in future and ends in future, active_time in future - Expected in future at active_start_time', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Subday Every 30 Minutes - Starts in future and ends in future, active_time now - Expected in future at active_start_time', 4, 1, 4, 30, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),


-- Weekly schedules
-- Template:
-- ('Weekly - Every Monday, Wednesday, and Friday at any time', 8, 42, 0, 0, 0, 1, 0, 0, 0, 0),
-- ('Weekly - Every Friday - Every 25 minutes within Friday', 8, 32, 4, 25, 0, 1, 0, 0, 0, 0),
-- Tests
('Weekly - Every day at 00:05 - Expected tomorrow at 00:05', 8, 127, 1, 0, 0, 1, 0, 0, 5, 0),
('Weekly - Every day at any time - Expected today at current time', 8, 127, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Sunday - Expected on Sunday at 00:00', 8, 1, 1, 0, 0, 0, 0, 0, 0, 0),
('Weekly - Every Sunday, starts today, finishes today - Expected today if Sunday or never', 8, 1, 1, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Weekly - Every Sunday, starts today, active_time in past - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Weekly - Every Sunday, starts today, active_time in future - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('Weekly - Every Sunday, starts in past - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('Weekly - Every Sunday, starts in past, active_time in past - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Weekly - Every Sunday, starts in past, active_time in future - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly - Every Sunday, starts in past, active_time now - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Weekly - Every Sunday, starts in future - Expected in future at 00:00', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Weekly - Every Sunday, starts in future, active_time in past - Expected in future at active_start_time', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Weekly - Every Sunday, starts in future, active_time in future - Expected in future at active_start_time', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly - Every Sunday, starts in future, active_time now - Expected in future at active_start_time', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Weekly - Every Sunday, starts in past and ends in past - Expected never', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Weekly - Every Sunday, starts in past and ends in past, active_time in past - Expected never', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Weekly - Every Sunday, starts in past and ends in past, active_time now - Expected never', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Weekly - Every Sunday, starts in past and ends in past, active_time in future - Expected never', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Weekly - Every Sunday, starts in past and ends in future - Expected on Sunday at 00:00', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Weekly - Every Sunday, starts in past and ends in future, active_time in past - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Weekly - Every Sunday, starts in past and ends in future, active_time in future - Expected on Sunday at active_start_time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly - Every Sunday, starts in past and ends in future, active_time now - Expected on Sunday at current time', 8, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('Weekly - Every Sunday, starts in future and ends in future - Expected in future at 00:00', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Weekly - Every Sunday, starts in future and ends in future, active_time in past - Expected in future at active_start_time', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Weekly - Every Sunday, starts in future and ends in future, active_time in future - Expected in future at active_start_time', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly - Every Sunday, starts in future and ends in future, active_time now - Expected in future at active_start_time', 8, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),

('Weekly - Every Sunday at any time - Expected on Sunday at current time (if today) or 00:00', 8, 1, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Monday at any time - Expected on Monday at current time (if today) or 00:00', 8, 2, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Tuesday at any time - Expected on Tuesday at current time (if today) or 00:00', 8, 4, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Wednesday at any time - Expected on Wednesday at current time (if today) or 00:00', 8, 8, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Thursday at any time - Expected on Thursday at current time (if today) or 00:00', 8, 16, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Friday at any time - Expected on Friday at current time (if today) or 00:00', 8, 32, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Saturday at any time - Expected on Saturday at current time (if today) or 00:00', 8, 64, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Workday at any time - Expected on Workday at current time (if today) or 00:00', 8, 62, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Weekend at any time - Expected on Weekend at current time (if today) or 00:00', 8, 65, 0, 0, 0, 1, 0, 0, 0, 0),
('Weekly - Every Monday, Wednesday, and Friday at any time - Expected on Monday, Wednesday, or Friday at current time (if today) or 00:00', 8, 42, 0, 0, 0, 1, 0, 0, 0, 0),

('Weekly - every week on Monday every 4 hours - Expected on nearest Monday', 8, 2, 8, 4, 0, 1,
	@PastDateInt, @FutureDateInt, 0, 235959),
('Weekly - every 3 weeks on Monday, Wednesday, Friday, Sunday at 5:00:00 PM - Expected schedule depends on previous_date/time', 8, 43, 1, 0, 0, 3,
	@CurrentDateInt, @FutureDateInt, 170000, 235959),

('Weekly All Days - Anytime - Expected at current date and time', 8, 127, 0, 0, 0, 0, 0, 0, 0, 0),

('Weekly All Days - Starts today, finishes today - Expected at current date and time', 8, 127, 0, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Weekly All Days - Starts today, active_time in past - Expected tomorrow at active_start_time', 8, 127, 0, 0, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Weekly All Days - Starts today, active_time in future - Expected today at active_start_time', 8, 127, 0, 0, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('Weekly All Days - Starts in past - Expected at current date and time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('Weekly All Days - Starts in past, active_time in past - Expected tomorrow at active_start_time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Weekly All Days - Starts in past, active_time in future - Expected today at active_start_time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly All Days - Starts in past, active_time now - Expected today at current time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Weekly All Days - Starts in future - Expected in future', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Weekly All Days - Starts in future, active_time in past - Expected in future at active_start_time', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Weekly All Days - Starts in future, active_time in future - Expected in future at active_start_time', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly All Days - Starts in future, active_time now - Expected in future at active_start_time', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Weekly All Days - Starts in past and ends in past - Expected never', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Weekly All Days - Starts in past and ends in past, active_time in past - Expected never', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Weekly All Days - Starts in past and ends in past, active_time now - Expected never', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Weekly All Days - Starts in past and ends in past, active_time in future - Expected never', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Weekly All Days - Starts in past and ends in future - Expected at current date and time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Weekly All Days - Starts in past and ends in future, active_time in past - Expected tomorrow at active_start_time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Weekly All Days - Starts in past and ends in future, active_time in future - Expected today at active_start_time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly All Days - Starts in past and ends in future, active_time now - Expected today at current time', 8, 127, 0, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('Weekly All Days - Starts in future and ends in future - Expected in future', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Weekly All Days - Starts in future and ends in future, active_time in past - Expected in future at active_start_time', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Weekly All Days - Starts in future and ends in future, active_time in future - Expected in future at active_start_time', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Weekly All Days - Starts in future and ends in future, active_time now - Expected in future at active_start_time', 8, 127, 0, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),


-- Monthly schedules
-- Template:
-- ('Monthly on the 15th day', 16, 15, 0, 0, 0, 1, 0, 0, 0, 0),
-- ('Monthly on the 20th day - Every 25 minutes within Friday', 16, 20, 4, 25, 0, 1, 0, 0, 0, 0),
-- Tests
('Monthly on the 15st day', 16, 15, 1, 0, 0, 1, 0, 0, 0, 0),
('Monthly on the 1st day - Anytime - Expected at current date and time', 16, 1, 1, 0, 0, 0, 0, 0, 0, 0),
('Monthly on the 1st day - Starts today, finishes today - Expected never (or today, if today is the 1st)', 16, 1, 1, 0, 0, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Monthly on the 1st day - Starts today, active_time in past - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Monthly on the 1st day - Starts today, active_time in future - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('Monthly on the 1st day - Starts in past - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, 0, 0, 0),
('Monthly on the 1st day - Starts in past, active_time in past - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Monthly on the 1st day - Starts in past, active_time in future - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly on the 1st day - Starts in past, active_time now - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Monthly on the 1st day - Starts in future - Expected on the 1st of future month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, 0, 0),
('Monthly on the 1st day - Starts in future, active_time in past - Expected on the 1st of future month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Monthly on the 1st day - Starts in future, active_time in future - Expected on the 1st of future month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly on the 1st day - Starts in future, active_time now - Expected on the 1st of future month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Monthly on the 1st day - Starts in past and ends in past - Expected never', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Monthly on the 1st day - Starts in past and ends in past, active_time in past - Expected never', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Monthly on the 1st day - Starts in past and ends in past, active_time now - Expected never', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Monthly on the 1st day - Starts in past and ends in past, active_time in future - Expected never', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Monthly on the 1st day - Starts in past and ends in future - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Monthly on the 1st day - Starts in past and ends in future, active_time in past - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Monthly on the 1st day - Starts in past and ends in future, active_time in future - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly on the 1st day - Starts in past and ends in future, active_time now - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('Monthly on the 1st day - Starts in future and ends in future - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Monthly on the 1st day - Starts in future and ends in future, active_time in past - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Monthly on the 1st day - Starts in future and ends in future, active_time in future - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly on the 1st day - Starts in future and ends in future, active_time now - Expected on the 1st of next month', 16, 1, 1, 0, 0, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),


('Monthly - every 2 month(s) on day 3 of that month at 6:00:00 AM - Expected on day 3 of the month OR in 2 months after last_execution_date/time', 16, 3, 1, 0, 0, 2,
	@PastDateInt, @FutureDateInt, 60000, 235959),
('Monthly - every 4 month(s) on day 7 of that month every 5 hour(s) between 5:00:00 AM and 6:00:00 PM - Expected on day 7 of the month OR in 4 months after last_execution_date/time', 16, 7, 8, 5, 0, 4,
	@PastDateInt, @FutureDateInt, 50000, 180000),
('Monthly - every 2 month(s) on day 15 of that month every 30 minute(s) between 2:00:00 PM and 6:00:00 PM - Expected on day 15 of the month OR in 2 months after last_execution_date/time', 16, 15, 4, 30, 0, 2,
	@PastDateInt, @FutureDateInt, 140000, 180000),

--- TODO: Valid until here

-- Monthly Relative schedules
-- Template:
-- ('Monthly Last Friday',                                                 32, 6, 1, 0, 16, 1, 0, 0, 0, 0),
-- ('Monthly - Every third(4) Saturday(7) of every 2 months every 5 hours(8)', 32, 7, 8, 5, 4, 2, 0, 0, 0, 0),
-- Tests
('Monthly Relative - First Monday at 00:00 - Expected at current date and time', 32, 2, 1, 0, 1, 0, 0, 0, 0, 0),
('Monthly Relative - Starts today, finishes today - Expected never (most likely)', 32, 2, 1, 0, 1, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),


('Monthly Last Friday - Anytime - Expected on Last Friday at 00:00', 32, 6, 1, 0, 16, 0, 0, 0, 0, 0),
('Monthly Last Friday - Starts today, finishes today - Expected never or today (if today is last friday)', 32, 6, 1, 0, 16, 0,
	@CurrentDateInt, @CurrentDateInt, 0, 0),

('Monthly Last Friday - Starts today, active_time in past - Expected on Last Friday at active_start_time', 32, 6, 1, 0, 16, 0,
	@CurrentDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Monthly Last Friday - Starts today, active_time in future - Expected on Last Friday at active_start_time', 32, 6, 1, 0, 16, 0,
	@CurrentDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),

('Monthly Last Friday - Starts in past - Expected on Last Friday', 32, 6, 1, 0, 16, 0,
	@PastDateInt, 0, 0, 0),
('Monthly Last Friday - Starts in past, active_time in past - Expected on Last Friday', 32, 6, 1, 0, 16, 0,
	@PastDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Monthly Last Friday - Starts in past, active_time in future - Expected on Last Friday', 32, 6, 1, 0, 16, 0,
	@PastDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly Last Friday - Starts in past, active_time now - Expected on Last Friday', 32, 6, 1, 0, 16, 0,
	@PastDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Monthly Last Friday - Starts in future - Expected in future', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, 0, 0, 0),
('Monthly Last Friday - Starts in future, active_time in past - Expected in future at active_start_time', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, 0, @PastPastTimeInt, @PastTimeInt),
('Monthly Last Friday - Starts in future, active_time in future - Expected in future at active_start_time', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, 0, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly Last Friday - Starts in future, active_time now - Expected in future at active_start_time', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, 0, @PastTimeInt, @FutureTimeInt),

('Monthly Last Friday - Starts in past and ends in past - Expected never', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @PastDateInt, 0, 0),
('Monthly Last Friday - Starts in past and ends in past, active_time in past - Expected never', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @PastTimeInt),
('Monthly Last Friday - Starts in past and ends in past, active_time now - Expected never', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @PastDateInt, @PastPastTimeInt, @FutureTimeInt),
('Monthly Last Friday - Starts in past and ends in past, active_time in future - Expected never', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @PastDateInt, @FutureTimeInt, @FutureFutureTimeInt),

('Monthly Last Friday - Starts in past and ends in future - Expectedon Last Friday', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @FutureDateInt, 0, 0),
('Monthly Last Friday - Starts in past and ends in future, active_time in past - Expected on Last Friday at active_start_time', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @FutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Monthly Last Friday - Starts in past and ends in future, active_time in future - Expected on Last Friday at active_start_time', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @FutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly Last Friday - Starts in past and ends in future, active_time now - Expected on Last Friday at active_start_time', 32, 6, 1, 0, 16, 0,
	@PastDateInt, @FutureDateInt, @PastTimeInt, @FutureTimeInt),

('Monthly Last Friday - Starts in future and ends in future - Expected in future', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, @FutureFutureDateInt, 0, 0),
('Monthly Last Friday - Starts in future and ends in future, active_time in past - Expected in future at active_start_time', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastPastTimeInt, @PastTimeInt),
('Monthly Last Friday - Starts in future and ends in future, active_time in future - Expected in future at active_start_time', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, @FutureFutureDateInt, @FutureTimeInt, @FutureFutureTimeInt),
('Monthly Last Friday - Starts in future and ends in future, active_time now - Expected in future at active_start_time', 32, 6, 1, 0, 16, 0,
	@FutureDateInt, @FutureFutureDateInt, @PastTimeInt, @FutureTimeInt),

('Monthly - Every first Tuesday of every 3 month(s) at 12:00:00 AM', 32, 3, 1, 0, 1, 3,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Every fourth Saturday of every 2 month(s) every 5 hour(s) between 12:00:00 AM and 11:59:59 PM', 32, 7, 8, 5, 8, 2,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Every third day of every 4 month(s) at 12:00:00 AM', 32, 8, 1, 0, 4, 4,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Every second weekday of every 3 month(s) at 2:00:00 PM', 32, 9, 1, 0, 2, 3,
	@PastDateInt, @FutureFutureDateInt, 140000, 235959),
('Monthly - Every last weekend of every 6 month(s) at 12:00:00 AM', 32, 10, 1, 0, 16, 6,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Every last Thursday of every 2 month(s) at 12:00:00 AM', 32, 5, 1, 0, 16, 2,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),

-- Test cases for Day (freq_interval=8)

('Monthly - First day of every month at 12:00:00 AM', 32, 8, 1, 0, 1, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Second day of every month at 12:00:00 AM', 32, 8, 1, 0, 2, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Third day of every month at 12:00:00 AM', 32, 8, 1, 0, 4, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Fourth day of every month at 12:00:00 AM', 32, 8, 1, 0, 8, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Last day of every month at 12:00:00 AM', 32, 8, 1, 0, 16, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),

-- Test cases for Weekday (freq_interval=9)
('Monthly - First weekday of every month at 12:00:00 AM', 32, 9, 1, 0, 1, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Second weekday of every month at 12:00:00 AM', 32, 9, 1, 0, 2, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Third weekday of every month at 12:00:00 AM', 32, 9, 1, 0, 4, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Fourth weekday of every month at 12:00:00 AM', 32, 9, 1, 0, 8, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Last weekday of every month at 12:00:00 AM', 32, 9, 1, 0, 16, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),

-- Test cases for Weekend day (freq_interval=10)
('Monthly - First weekend day of every month at 12:00:00 AM', 32, 10, 1, 0, 1, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Second weekend day of every month at 12:00:00 AM', 32, 10, 1, 0, 2, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Third weekend day of every month at 12:00:00 AM', 32, 10, 1, 0, 4, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Fourth weekend day of every month at 12:00:00 AM', 32, 10, 1, 0, 8, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Last weekend day of every month at 12:00:00 AM', 32, 10, 1, 0, 16, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),

-- Test cases for specific days of week with different occurrences
('Monthly - First Monday of every month at 12:00:00 AM', 32, 2, 1, 0, 1, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Second Tuesday of every month at 12:00:00 AM', 32, 3, 1, 0, 2, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Third Wednesday of every month at 12:00:00 AM', 32, 4, 1, 0, 4, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Fourth Thursday of every month at 12:00:00 AM', 32, 5, 1, 0, 8, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Last Sunday of every month at 12:00:00 AM', 32, 1, 1, 0, 16, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),

-- Test cases with different subday frequencies
('Monthly - First Monday of every month every 30 minutes', 32, 2, 4, 30, 1, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Second Tuesday of every month every 2 hours', 32, 3, 8, 2, 2, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Third Wednesday of every month every 15 seconds', 32, 4, 2, 15, 4, 1,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),

-- Test cases with different recurrence factors
('Monthly - First Thursday of every 3 months at 12:00:00 AM', 32, 5, 1, 0, 1, 3,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Second Friday of every 6 months at 12:00:00 AM', 32, 6, 1, 0, 2, 6,
	@PastDateInt, @FutureFutureDateInt, 0, 235959),
('Monthly - Last Saturday of every 12 months at 12:00:00 AM', 32, 7, 1, 0, 16, 12,
	@PastDateInt, @FarFutureDateInt, 0, 235959),

-- Test cases with specific time ranges
('Monthly - First Monday of every month between 8:00:00 AM and 5:00:00 PM', 32, 2, 0, 0, 1, 1,
	@PastDateInt, @FutureFutureDateInt, 80000, 170000),
('Monthly - Second Tuesday of every month at 9:30:00 AM', 32, 3, 1, 0, 2, 1,
	@PastDateInt, @FutureFutureDateInt, 93000, 235959),
('Monthly - Third Wednesday of every month every 1 hour between 10:00:00 AM and 4:00:00 PM', 32, 4, 8, 1, 4, 1,
	@PastDateInt, @FutureFutureDateInt, 100000, 160000);
