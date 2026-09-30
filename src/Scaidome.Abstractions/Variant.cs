using System.Globalization;
using System.Text.Json.Serialization;

namespace Scaidome;

/// <summary>
/// One piece of data of a given <see cref="VariantKind"/>. The default is no value. A variant never holds what its kind cannot
/// mean: building one that breaks the rules throws, and converting untrusted input goes through
/// <see cref="VariantConversion"/> or <see cref="VariantTextForm"/>, which report failure instead.
/// </summary>
[JsonConverter(typeof(VariantJsonConverter))]
public readonly struct Variant : IEquatable<Variant>
{
    private readonly double _number;
    private readonly string? _text;
    private readonly DateTime _dateTime;

    private Variant(VariantKind kind, double number, string? text, DateTime dateTime)
    {
        Kind = kind;
        _number = number;
        _text = text;
        _dateTime = dateTime;
    }

    /// <summary>No value.</summary>
    public static Variant Null => default;

    public VariantKind Kind { get; }

    public bool IsNull => Kind == VariantKind.Null;

    public VariantStorage Storage => StorageOf(Kind);

    public bool HasNumber => Storage == VariantStorage.Number;

    public bool HasText => Storage == VariantStorage.Text;

    public bool HasDateTime => Storage == VariantStorage.DateTime;

    public static VariantStorage StorageOf(VariantKind kind) => kind switch
    {
        VariantKind.Null => VariantStorage.None,
        VariantKind.Int or VariantKind.Double or VariantKind.Bool or VariantKind.Epoch => VariantStorage.Number,
        VariantKind.String or VariantKind.Json or VariantKind.Color or VariantKind.Matrix => VariantStorage.Text,
        VariantKind.DateTime => VariantStorage.DateTime,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown variant kind."),
    };

    public static Variant FromInt(int value) => new(VariantKind.Int, value, null, default);

    public static Variant FromDouble(double value) => new(VariantKind.Double, RequireFinite(value), null, default);

    public static Variant FromBool(bool value) => new(VariantKind.Bool, value ? 1 : 0, null, default);

    public static Variant FromEpoch(double value) => new(VariantKind.Epoch, RequireFinite(value), null, default);

    /// <summary>
    /// A date and time, held in UTC. A local time is converted; a time without a kind is taken to be UTC already.
    /// </summary>
    public static Variant FromDateTime(DateTime value) => new(VariantKind.DateTime, 0, null, UtcTime.ToUtc(value));

    /// <summary>
    /// A date and time with an offset, converted to UTC by its offset alone, never through the machine's local time.
    /// </summary>
    public static Variant FromDateTime(DateTimeOffset value) => new(VariantKind.DateTime, 0, null, value.UtcDateTime);

    public static Variant FromString(string text) => FromText(VariantKind.String, text);

    public static Variant FromColor(string text) => FromText(VariantKind.Color, text);

    public static Variant FromMatrix(string text) => FromText(VariantKind.Matrix, text);

    /// <summary>A JSON value from text that is checked to hold exactly one well-formed JSON value.</summary>
    public static Variant FromJson(string json) => FromText(VariantKind.Json, json);

    /// <summary>
    /// A JSON value whose text a parser or writer produced within the same operation, so it is not checked again. Text from
    /// outside the process must go through <see cref="FromJson"/>.
    /// </summary>
    public static Variant FromTrustedJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return new Variant(VariantKind.Json, 0, json, default);
    }

    /// <summary>
    /// A value of a kind stored as text. Text could mean any of four kinds, so the kind is always named.
    /// </summary>
    public static Variant FromText(VariantKind kind, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (StorageOf(kind) != VariantStorage.Text)
        {
            throw new ArgumentException($"Kind {kind} is not stored as text.", nameof(kind));
        }

        if (kind == VariantKind.Json && !JsonText.IsWellFormed(text))
        {
            throw new ArgumentException("Text is not one well-formed JSON value.", nameof(text));
        }

        return new Variant(kind, 0, text, default);
    }

    public static implicit operator Variant(int value) => FromInt(value);

    public static implicit operator Variant(double value) => FromDouble(value);

    public static implicit operator Variant(bool value) => FromBool(value);

    public static implicit operator Variant(DateTime value) => FromDateTime(value);

    public static implicit operator Variant(DateTimeOffset value) => FromDateTime(value);

    /// <summary>The number this value holds. Allowed for every kind stored as a number, a boolean reading as 1 or 0.</summary>
    public double AsDouble()
    {
        RequireStorage(VariantStorage.Number);
        return _number;
    }

    /// <summary>The number this value holds as a 32-bit integer; it must be whole and fit.</summary>
    public int AsInt()
    {
        RequireStorage(VariantStorage.Number);
        if (_number != Math.Floor(_number) || _number < int.MinValue || _number > int.MaxValue)
        {
            throw new InvalidOperationException($"Value {ToString()} is not a 32-bit integer.");
        }

        return (int)_number;
    }

    /// <summary>The value as a boolean: true when the number it holds is not zero.</summary>
    public bool AsBool()
    {
        RequireStorage(VariantStorage.Number);
        return _number != 0;
    }

    public string AsText()
    {
        RequireStorage(VariantStorage.Text);
        return _text ?? string.Empty;
    }

    public DateTime AsDateTime()
    {
        RequireStorage(VariantStorage.DateTime);
        return _dateTime;
    }

    /// <summary>The zero of a kind, which stands in where a value of that kind is needed and none is configured.</summary>
    public static Variant ZeroOf(VariantKind kind) => kind switch
    {
        VariantKind.Null => Null,
        VariantKind.Int => FromInt(0),
        VariantKind.Double => FromDouble(0),
        VariantKind.Epoch => FromEpoch(0),
        VariantKind.Bool => FromBool(false),
        VariantKind.String or VariantKind.Color or VariantKind.Matrix => FromText(kind, string.Empty),
        VariantKind.Json => FromTrustedJson("{}"),
        VariantKind.DateTime => FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown variant kind."),
    };

    /// <summary>Equal only when both kinds and contents are equal: integer 1 is not number 1.</summary>
    public bool Equals(Variant other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        return Storage switch
        {
            VariantStorage.None => true,
            VariantStorage.Number => _number == other._number,
            VariantStorage.Text => string.Equals(_text ?? string.Empty, other._text ?? string.Empty, StringComparison.Ordinal),
            VariantStorage.DateTime => _dateTime.Ticks == other._dateTime.Ticks,
            _ => false,
        };
    }

    /// <summary>
    /// Compares with another variant, or with a plain value by storage: a number or boolean against a value stored as a number,
    /// text against a value stored as text, and a date and time against a date and time.
    /// </summary>
    public override bool Equals(object? obj) => obj switch
    {
        Variant other => Equals(other),
        int i => Equals(i),
        long l => Equals(l),
        float f => Equals(f),
        double d => Equals(d),
        bool b => Equals(b),
        string s => Equals(s),
        DateTime dt => Equals(dt),
        DateTimeOffset dto => Equals(dto),
        _ => false,
    };

    // The plain overloads exist so that a plain argument binds here rather than to Equals(Variant) through the implicit
    // conversion, which would compare kinds and make integer 1 unequal to the number 1.

    /// <summary>A value stored as a number equals the same numeric value, and is never truncated to match.</summary>
    public bool Equals(int value) => HasNumber && _number == value;

    public bool Equals(long value) => HasNumber && _number == value;

    public bool Equals(float value) => HasNumber && _number == value;

    public bool Equals(double value) => HasNumber && _number == value;

    /// <summary>A value stored as a number equals a boolean when its being non-zero matches it.</summary>
    public bool Equals(bool value) => HasNumber && (_number != 0) == value;

    /// <summary>A value stored as text equals the same text.</summary>
    public bool Equals(string? value) => HasText && value is not null && string.Equals(_text ?? string.Empty, value, StringComparison.Ordinal);

    public bool Equals(DateTime value) => HasDateTime && _dateTime.Ticks == UtcTime.ToUtc(value).Ticks;

    public bool Equals(DateTimeOffset value) => HasDateTime && _dateTime.Ticks == value.UtcDateTime.Ticks;

    public override int GetHashCode() => Storage switch
    {
        VariantStorage.None => 0,
        VariantStorage.Number => HashCode.Combine(Kind, _number),
        VariantStorage.Text => HashCode.Combine(Kind, StringComparer.Ordinal.GetHashCode(_text ?? string.Empty)),
        VariantStorage.DateTime => HashCode.Combine(Kind, _dateTime.Ticks),
        _ => 0,
    };

    public static bool operator ==(Variant left, Variant right) => left.Equals(right);

    public static bool operator !=(Variant left, Variant right) => !left.Equals(right);

    /// <summary>The diagnostic text: the content alone, in the invariant culture, without the kind.</summary>
    public override string ToString() => Kind switch
    {
        VariantKind.Null => "null",
        VariantKind.Bool => _number != 0 ? "true" : "false",
        VariantKind.Int => ((int)_number).ToString(CultureInfo.InvariantCulture),
        VariantKind.Double or VariantKind.Epoch => _number.ToString("R", CultureInfo.InvariantCulture),
        VariantKind.DateTime => _dateTime.ToString("O", CultureInfo.InvariantCulture),
        _ => _text ?? string.Empty,
    };

    private void RequireStorage(VariantStorage storage)
    {
        if (Storage != storage)
        {
            throw new InvalidOperationException($"A value of kind {Kind} is not stored as {storage}.");
        }
    }

    private static double RequireFinite(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A number must be finite.");
        }

        return value;
    }
}
