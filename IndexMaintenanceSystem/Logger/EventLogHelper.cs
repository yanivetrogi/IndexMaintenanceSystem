using System.Runtime.Versioning;
using System.Diagnostics;
using System.Reflection;

namespace IndexMaintenanceSystem.Logger
{
    public static class EventLogHelper
    {
        private const string Source = "IndexMaintenanceSystem";
        private const string LogName = "Application";

        [SupportedOSPlatform("windows")]
        public static void LogInformation(string message)
        {
            Log(message, EventLogEntryType.Information);
        }

        [SupportedOSPlatform("windows")]
        public static void LogError(string message, Exception? ex = null)
        {
            var fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\n\nException: {ex.GetType().Name}: {ex.Message}\nStack Trace:\n{ex.StackTrace}";
            }
            Log(fullMessage, EventLogEntryType.Error);
        }

        [SupportedOSPlatform("windows")]
        private static void Log(string message, EventLogEntryType type)
        {
            try
            {
                if (!OperatingSystem.IsWindows()) return;

                // Note: Source creation requires administrative privileges.
                if (!EventLog.SourceExists(Source))
                {
                    EventLog.CreateEventSource(Source, LogName);
                }
                EventLog.WriteEntry(Source, message, type);
            }
            catch
            {
                // Silently fail if we can't write to Event Log (e.g. permission issues)
            }
        }

        public static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(4) ?? "4.3.2.0";
        }
    }
}
