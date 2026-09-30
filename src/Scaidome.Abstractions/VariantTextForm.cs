using System.Globalization;

namespace Scaidome;

/// <summary>
/// The text form with kind: a value written as its content and its kind's name, so the kind survives being stored as text.
/// Reading reports failure rather than throwing, so a record that cannot be read can be skipped.
/// </summary>
public static class VariantTextForm
{
    private static readonly string[] KindNames = Enum.GetNames<VariantKind>();

    /// <summary>The value's content and its kind's name.</summary>
    public static (string Text, string Kind) Write(Variant value)
    {
        var text = value.Kind switch
        {
            VariantKind.Null => string.Empty,
            VariantKind.Bool => value.AsBool() ? "True" : "False",
            VariantKind.Int => value.AsInt().ToString(CultureInfo.InvariantCulture),
            VariantKind.Double or VariantKind.Epoch => value.AsDouble().ToString("R", CultureInfo.InvariantCulture),
            VariantKind.DateTime => value.AsDateTime().ToString("O", CultureInfo.InvariantCulture),
            _ => value.AsText(),
        };
        return (text, value.Kind.ToString());
    }

    /// <summary>Reads a kind name, without regard to case. A number or a list of names is not a kind name.</summary>
    public static bool TryReadKind(string? kindName, out VariantKind kind)
    {
        // Enum parsing would accept "1" and "Int, Double"; only an exact name is a kind name.
        foreach (var name in KindNames)
        {
            if (string.Equals(name, kindName, StringComparison.OrdinalIgnoreCase))
            {
                kind = Enum.Parse<VariantKind>(name);
                return true;
            }
        }

        kind = VariantKind.Null;
        return false;
    }

    /// <summary>Reads a value back from its text and kind name.</summary>
    public static bool TryRead(string? text, string? kindName, out Variant value)
    {
        value = Variant.Null;
        if (!TryReadKind(kindName, out var kind))
        {
            return false;
        }

        text ??= string.Empty;
        switch (kind)
        {
            case VariantKind.Null:
                return true;
            case VariantKind.Int:
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                {
                    return false;
                }

                value = Variant.FromInt(i);
                return true;
            case VariantKind.Double:
            case VariantKind.Epoch:
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) || !double.IsFinite(d))
                {
                    return false;
                }

                value = kind == VariantKind.Double ? Variant.FromDouble(d) : Variant.FromEpoch(d);
                return true;
            case VariantKind.Bool:
                if (!bool.TryParse(text, out var b))
                {
                    return false;
                }

                value = Variant.FromBool(b);
                return true;
            case VariantKind.DateTime:
                if (!DateTime.TryParse(
                        text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var dt))
                {
                    return false;
                }

                value = Variant.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                return true;
            case VariantKind.Json:
                if (!JsonText.IsWellFormed(text))
                {
                    return false;
                }

                value = Variant.FromTrustedJson(text);
                return true;
            default:
                // Colour and matrix text is read back exactly as written, even when it describes nothing.
                value = Variant.FromText(kind, text);
                return true;
        }
    }
}
