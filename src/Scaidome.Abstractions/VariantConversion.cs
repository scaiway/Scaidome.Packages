using System.Globalization;

namespace Scaidome;

/// <summary>
/// Converts stored or configured values, and plain platform values, to a declared kind. A value already of the kind is
/// unchanged; any other conversion either follows the values specification's table or fails.
/// </summary>
public static class VariantConversion
{
    /// <summary>A plain whole number, number, boolean or date and time as a value of its own kind.</summary>
    public static bool TryFromPlain(object? plain, out Variant value)
    {
        value = Variant.Null;
        switch (plain)
        {
            case null:
                return true;
            case Variant v:
                value = v;
                return true;
            case int or short or sbyte or byte or ushort:
                value = Variant.FromInt(System.Convert.ToInt32(plain, CultureInfo.InvariantCulture));
                return true;
            case long or uint or ulong:
                var whole = System.Convert.ToDecimal(plain, CultureInfo.InvariantCulture);
                value = whole is >= int.MinValue and <= int.MaxValue
                    ? Variant.FromInt((int)whole)
                    : Variant.FromDouble((double)whole);
                return true;
            case float or double or decimal:
                var number = System.Convert.ToDouble(plain, CultureInfo.InvariantCulture);
                if (!double.IsFinite(number))
                {
                    return false;
                }

                value = Variant.FromDouble(number);
                return true;
            case bool b:
                value = Variant.FromBool(b);
                return true;
            case DateTime dt:
                value = Variant.FromDateTime(dt);
                return true;
            case DateTimeOffset dto:
                value = Variant.FromDateTime(dto);
                return true;
            default:
                // Text could mean any of four kinds, so it never becomes a value without a kind.
                return false;
        }
    }

    /// <summary>Converts a value to a kind, reporting failure.</summary>
    public static bool TryConvert(Variant value, VariantKind kind, out Variant result)
    {
        if (value.Kind == kind)
        {
            result = value;
            return true;
        }

        var plain = PlainReading.Of(value);
        return TryConvert(plain, kind, out result);
    }

    /// <summary>Converts a plain platform value, or a boxed value, to a kind, reporting failure.</summary>
    public static bool TryConvert(object? plain, VariantKind kind, out Variant result)
    {
        if (plain is Variant variant)
        {
            return TryConvert(variant, kind, out result);
        }

        if (!PlainReading.TryOf(plain, out var reading))
        {
            result = Variant.Null;
            return false;
        }

        return TryConvert(reading, kind, out result);
    }

    /// <summary>Converts a value to a kind, throwing <see cref="FormatException"/> when it cannot.</summary>
    public static Variant Convert(Variant value, VariantKind kind) =>
        TryConvert(value, kind, out var result)
            ? result
            : throw new FormatException($"Value {value} of kind {value.Kind} cannot be converted to {kind}.");

    /// <summary>Converts a plain value to a kind, throwing <see cref="FormatException"/> when it cannot.</summary>
    public static Variant Convert(object? plain, VariantKind kind) =>
        TryConvert(plain, kind, out var result)
            ? result
            : throw new FormatException($"Value {ToInvariantText(plain)} cannot be converted to {kind}.");

    /// <summary>
    /// A plain value as text in the invariant culture. It cannot fail: a date and time is written in round-trip form in UTC, and
    /// nothing becomes nothing.
    /// </summary>
    public static string? ToInvariantText(object? plain) => plain switch
    {
        null => null,
        string s => s,
        DateTime dt => UtcTime.ToUtc(dt).ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => plain.ToString(),
    };

    private static bool TryConvert(PlainReading reading, VariantKind kind, out Variant result)
    {
        result = Variant.Null;
        switch (kind)
        {
            case VariantKind.Null:
                return reading.Storage == VariantStorage.None;
            case VariantKind.Int:
                if (reading.Storage == VariantStorage.Number)
                {
                    var n = reading.Number;
                    if (n != Math.Floor(n) || n < int.MinValue || n > int.MaxValue)
                    {
                        return false;
                    }

                    result = Variant.FromInt((int)n);
                    return true;
                }

                if (reading.Storage == VariantStorage.Text
                    && int.TryParse(reading.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                {
                    result = Variant.FromInt(i);
                    return true;
                }

                return false;
            case VariantKind.Double:
            case VariantKind.Epoch:
                if (!TryReadNumber(reading, out var number))
                {
                    return false;
                }

                result = kind == VariantKind.Double ? Variant.FromDouble(number) : Variant.FromEpoch(number);
                return true;
            case VariantKind.Bool:
                if (reading.Storage == VariantStorage.Text && bool.TryParse(reading.Text, out var b))
                {
                    result = Variant.FromBool(b);
                    return true;
                }

                if (!TryReadNumber(reading, out var truth))
                {
                    return false;
                }

                result = Variant.FromBool(truth != 0);
                return true;
            case VariantKind.String:
            case VariantKind.Color:
            case VariantKind.Matrix:
                if (reading.Storage != VariantStorage.Text)
                {
                    return false;
                }

                result = Variant.FromText(kind, reading.Text);
                return true;
            case VariantKind.Json:
                if (reading.Storage != VariantStorage.Text || !JsonText.IsWellFormed(reading.Text))
                {
                    return false;
                }

                result = Variant.FromTrustedJson(reading.Text);
                return true;
            case VariantKind.DateTime:
                // Neither a number nor an epoch converts: the epoch's unit belongs to the interface.
                if (reading.Storage != VariantStorage.DateTime)
                {
                    return false;
                }

                result = Variant.FromDateTime(reading.DateTime);
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadNumber(PlainReading reading, out double number)
    {
        number = 0;
        if (reading.Storage == VariantStorage.Number)
        {
            number = reading.Number;
            return double.IsFinite(number);
        }

        return reading.Storage == VariantStorage.Text
            && double.TryParse(reading.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            && double.IsFinite(number);
    }

    /// <summary>The plain value a storage holds, which is what a conversion reads.</summary>
    private readonly record struct PlainReading(VariantStorage Storage, double Number, string Text, DateTime DateTime)
    {
        public static PlainReading Of(Variant value) => value.Storage switch
        {
            VariantStorage.Number => new PlainReading(VariantStorage.Number, value.AsDouble(), string.Empty, default),
            VariantStorage.Text => new PlainReading(VariantStorage.Text, 0, value.AsText(), default),
            VariantStorage.DateTime => new PlainReading(VariantStorage.DateTime, 0, string.Empty, value.AsDateTime()),
            _ => new PlainReading(VariantStorage.None, 0, string.Empty, default),
        };

        public static bool TryOf(object? plain, out PlainReading reading)
        {
            switch (plain)
            {
                case null:
                    reading = new PlainReading(VariantStorage.None, 0, string.Empty, default);
                    return true;
                case string s:
                    reading = new PlainReading(VariantStorage.Text, 0, s, default);
                    return true;
                case bool b:
                    reading = new PlainReading(VariantStorage.Number, b ? 1 : 0, string.Empty, default);
                    return true;
                case DateTime dt:
                    reading = new PlainReading(VariantStorage.DateTime, 0, string.Empty, UtcTime.ToUtc(dt));
                    return true;
                case DateTimeOffset dto:
                    reading = new PlainReading(VariantStorage.DateTime, 0, string.Empty, dto.UtcDateTime);
                    return true;
                case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    reading = new PlainReading(
                        VariantStorage.Number,
                        System.Convert.ToDouble(plain, CultureInfo.InvariantCulture),
                        string.Empty,
                        default);
                    return true;
                default:
                    reading = default;
                    return false;
            }
        }
    }
}
