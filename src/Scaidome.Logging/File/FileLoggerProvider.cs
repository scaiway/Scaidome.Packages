using Microsoft.Extensions.Logging;

namespace Scaidome.Logging.File;

/// <summary>
/// A logging provider that writes to a file. The host binds <see cref="FileLoggerOptions"/> and hands them over when it
/// constructs the provider; every logger it creates writes to the same file through the one queue it owns.
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerOptions _options;
    private readonly FileLogQueue _queue;

    public FileLoggerProvider(FileLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _queue = new FileLogQueue(options.Path, options.FileSizeLimitBytes, options.MaxRollingFiles);
    }

    /// <summary>The full path of the live file.</summary>
    public string FilePath => _queue.Path;

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, GetLevel(categoryName), _queue);

    /// <summary>
    /// Stops the worker once it has written what is already queued, within a bounded wait. Disposing again does nothing.
    /// </summary>
    public void Dispose() => _queue.Dispose();

    /// <summary>
    /// The longest configured category that the category starts with decides, compared without regard to case; otherwise the
    /// default level, which is Information when the settings name none. Internal for tests.
    /// </summary>
    internal LogLevel GetLevel(string categoryName)
    {
        var levels = _options.LogLevel ?? [];
        string? best = null;
        var level = LogLevel.Information;
        foreach (var (category, configured) in levels)
        {
            if (string.Equals(category, FileLoggerOptions.DefaultCategory, StringComparison.OrdinalIgnoreCase))
            {
                if (best is null)
                {
                    level = configured;
                }

                continue;
            }

            if (categoryName.StartsWith(category, StringComparison.OrdinalIgnoreCase)
                && (best is null || category.Length > best.Length))
            {
                best = category;
                level = configured;
            }
        }

        return level;
    }
}
