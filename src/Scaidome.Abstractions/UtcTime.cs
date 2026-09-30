namespace Scaidome;

/// <summary>Brings a date and time to UTC the one way every value form agrees on.</summary>
internal static class UtcTime
{
    /// <summary>
    /// A local time is converted; a time without a kind is taken to be UTC already, because nothing says which zone it was
    /// written in and the stores hold UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
