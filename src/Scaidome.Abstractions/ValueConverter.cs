using System.Globalization;

namespace Scaidome;

/// <summary>
/// Reads a raw device value as a plain number. Unlike <see cref="VariantConversion"/> it produces no variant, so it can report
/// what a variant cannot hold: a non-finite double converts, and the caller decides what that measurement means. Text is
/// read in the invariant culture.
/// </summary>
public static class ValueConverter
{
    /// <summary>
    /// A whole number, a number truncated toward zero, or integer text, as an int. A value that does not fit is a failed
    /// conversion, never a wrapped one.
    /// </summary>
    public static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;
        switch (value)
        {
            case null:
                return false;
            case int i:
                result = i;
                return true;
            case byte or sbyte or short or ushort:
                result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return true;
            case uint or long or ulong or float or double or decimal:
                return TryTruncate(value, out result);
            case string text:
                return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            case IConvertible:
                return TryTruncate(value, out result);
            default:
                return false;
        }
    }

    /// <summary>Any number, or number text, as a double. NaN and the infinities convert, as numbers or as their invariant text.</summary>
    public static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;
        switch (value)
        {
            case null:
                return false;
            case double d:
                result = d;
                return true;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or decimal:
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            case string text:
                return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
            case IConvertible:
                try
                {
                    result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
                {
                    return false;
                }
            default:
                return false;
        }
    }

    private static bool TryTruncate(object value, out int result)
    {
        result = 0;
        double number;
        try
        {
            number = Math.Truncate(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }

        // NaN fails both comparisons, so it is refused with the values that do not fit.
        if (!(number >= int.MinValue && number <= int.MaxValue))
        {
            return false;
        }

        result = (int)number;
        return true;
    }
}
