using System.Data;
using System.Globalization;
using Dapper;

namespace Scaidome.Dapper;

/// <summary>
/// Reads a time that SQLite holds as text, as UTC, whatever the machine's culture. The driver writes the date, a space and the
/// time with up to seven fractional digits and no zone; the other listed forms are what hand-written rows and other tools
/// leave behind.
/// </summary>
public sealed class TimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    /// <summary>The form the rules themselves write a time in.</summary>
    public const string WriteFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private static readonly string[] Formats =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
    ];

    public override DateTime Parse(object value) => value switch
    {
        DateTime dateTime => ToUtc(dateTime),
        DateTimeOffset offset => offset.UtcDateTime,
        string text => ParseText(text),
        _ => throw new DataException($"A stored {value.GetType().Name} is not a time."),
    };

    /// <summary>
    /// The rules only read. The parameter keeps the time itself, so the database driver binds it in its own form.
    /// </summary>
    public override void SetValue(IDbDataParameter parameter, DateTime value) => parameter.Value = value;

    /// <summary>Writes a time in UTC as the date, T, the time with milliseconds and Z, in the invariant culture.</summary>
    public static string Format(DateTime value) => ToUtc(value).ToString(WriteFormat, CultureInfo.InvariantCulture);

    /// <summary>Reads a stored time text as UTC. Internal for tests.</summary>
    internal static DateTime ParseText(string text)
    {
        const DateTimeStyles styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        if (DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture, styles, out var exact))
        {
            return DateTime.SpecifyKind(exact, DateTimeKind.Utc);
        }

        // Any other text that reads as a date and time is read as UTC too; one with an offset is converted by it.
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, styles, out var loose))
        {
            return DateTime.SpecifyKind(loose, DateTimeKind.Utc);
        }

        throw new DataException($"Stored text '{text}' is not a time.");
    }

    /// <summary>A time without a zone is taken to be UTC; a local time is converted.</summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
