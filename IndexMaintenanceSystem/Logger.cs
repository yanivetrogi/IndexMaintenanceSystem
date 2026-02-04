namespace IndexMaintenanceSystem.Logger;

public class FileLoggerProvider : ILoggerProvider
{
    private string? path;
    private LogLevel level;

    public FileLoggerProvider(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        path = configuration["File:Path"];

        if (path != null && !Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        Enum.TryParse(configuration["File:LogLevel:Default"], out level);
    }
    public ILogger CreateLogger(string categoryName)
    {
        if (path == null)
        {
            throw new InvalidOperationException("Path not found");
        }

        return new FileLogger(path, level);
    }

    public void Dispose()
    {
    }
}

public class FileLogger : ILogger
{
    private string filePath;
    private static object _lock = new object();
    public FileLogger(string path, LogLevel level)
    {
        filePath = path;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        //return logLevel == LogLevel.Trace;
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        if (formatter != null)
        {
            lock (_lock)
            {
                string fullFilePath = Path.Combine(filePath, DateTime.Now.ToString("yyyy-MM-dd") + "_log.txt");
                var n = Environment.NewLine;
                string exc = "";
                Directory.CreateDirectory(filePath);
                if (exception != null) exc = n + exception.GetType() + ": " + exception.Message + n + exception.StackTrace + n;
                File.AppendAllText(fullFilePath, logLevel.ToString() + ": " + DateTime.Now.ToString("HH:mm:ss:fff") + " " + formatter(state, exception ?? new Exception()) + n + exc);
            }
        }
    }
}

