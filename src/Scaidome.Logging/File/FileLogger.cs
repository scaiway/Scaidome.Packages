using Microsoft.Extensions.Logging;

namespace Scaidome.Logging.File;

/// <summary>A logger that queues its entries for the provider's one file. Its level is decided once, when it is created.</summary>
public sealed class FileLogger : ILogger
{
    private readonly string _category;
    private readonly LogLevel _minimumLevel;
    private readonly FileLogQueue _queue;

    internal FileLogger(string category, LogLevel minimumLevel, FileLogQueue queue)
    {
        _category = category;
        _minimumLevel = minimumLevel;
        _queue = queue;
    }

    /// <summary>
    /// Known defect: scopes are not supported and beginning one returns nothing, so a caller that disposes the result must
    /// tolerate null. The fix is to return a no-op scope.
    /// </summary>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    /// <summary>The level that turns logging off is never an entry's level.</summary>
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);
        var message = formatter(state, exception);
        _queue.Enqueue(new FileLogEntry(DateTimeOffset.Now, logLevel, _category, eventId, message, exception));
    }
}
