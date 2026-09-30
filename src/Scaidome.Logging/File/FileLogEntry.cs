using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Scaidome.Logging.File;

/// <summary>One queued log entry, formatted when it is written.</summary>
internal sealed record FileLogEntry
{
    public FileLogEntry(DateTimeOffset timestamp, LogLevel level, string category, EventId eventId, string message, Exception? exception)
    {
        Timestamp = timestamp;
        Level = level;
        Category = category;
        EventId = eventId;
        Message = message;
        Exception = exception;
    }

    public DateTimeOffset Timestamp { get; }

    public LogLevel Level { get; }

    public string Category { get; }

    public EventId EventId { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    /// <summary>
    /// The entry's lines: the local time with its offset, the level in brackets, the category with a non-zero event id in
    /// brackets, a colon and the message, then the exception's full text, which the message leaves out.
    /// </summary>
    public string Format()
    {
        var culture = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();
        builder.Append(Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffzzz", culture));
        builder.Append(" [").Append(Level.ToString()).Append("] ");
        builder.Append(Category);
        if (EventId.Id != 0)
        {
            builder.Append('[').Append(EventId.Id.ToString(culture)).Append(']');
        }

        builder.Append(": ").Append(Message);
        builder.Append(Environment.NewLine);
        if (Exception is not null)
        {
            builder.Append(Exception).Append(Environment.NewLine);
        }

        return builder.ToString();
    }
}
