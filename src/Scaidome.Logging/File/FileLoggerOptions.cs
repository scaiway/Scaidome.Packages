using Microsoft.Extensions.Logging;

namespace Scaidome.Logging.File;

/// <summary>File logging settings, bound by the host from configuration and handed to the provider.</summary>
public sealed class FileLoggerOptions
{
    public const string DefaultPath = "/logs/app.log";
    public const long DefaultFileSizeLimitBytes = 10 * 1024 * 1024;
    public const int DefaultMaxRollingFiles = 3;
    public const string DefaultCategory = "Default";

    public string Path { get; set; } = DefaultPath;

    public long FileSizeLimitBytes { get; set; } = DefaultFileSizeLimitBytes;

    /// <summary>The number of files kept, the live file included.</summary>
    public int MaxRollingFiles { get; set; } = DefaultMaxRollingFiles;

    /// <summary>Levels by category name, with Default among them. Default is Information when not given.</summary>
    public Dictionary<string, LogLevel> LogLevel { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [DefaultCategory] = Microsoft.Extensions.Logging.LogLevel.Information,
    };
}
