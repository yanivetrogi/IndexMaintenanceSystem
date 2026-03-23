
-- When running the service as a LOcal System.

use master
-- Grant SYSTEM login and access to the IMS database
CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS;

USE [IndexMaintenanceSystem];
CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM];
ALTER ROLE [db_owner] ADD MEMBER [NT AUTHORITY\SYSTEM];