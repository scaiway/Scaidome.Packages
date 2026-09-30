namespace Scaidome;

/// <summary>
/// The nine type words a definition declares a value's kind with, and the kind each maps to. Words match without regard to
/// case and are kept in lower case. Checking a given value against its type belongs to the settings API.
/// </summary>
public static class DeclaredTypes
{
    public const string Int = "int";
    public const string Number = "number";
    public const string Boolean = "boolean";
    public const string String = "string";
    public const string Json = "json";
    public const string DateTime = "datetime";
    public const string Epoch = "epoch";
    public const string Color = "color";
    public const string Matrix = "matrix";

    private static readonly (string Type, VariantKind Kind)[] Map =
    [
        (Int, VariantKind.Int),
        (Number, VariantKind.Double),
        (Boolean, VariantKind.Bool),
        (String, VariantKind.String),
        (Json, VariantKind.Json),
        (DateTime, VariantKind.DateTime),
        (Epoch, VariantKind.Epoch),
        (Color, VariantKind.Color),
        (Matrix, VariantKind.Matrix),
    ];

    /// <summary>Every type word, in the order of the values specification.</summary>
    public static IReadOnlyList<string> All { get; } = Map.Select(entry => entry.Type).ToArray();

    /// <summary>The lower-case spelling of a type word, or false when the word is not a type.</summary>
    public static bool TryNormalize(string? word, out string type)
    {
        foreach (var entry in Map)
        {
            if (string.Equals(entry.Type, word, StringComparison.OrdinalIgnoreCase))
            {
                type = entry.Type;
                return true;
            }
        }

        type = string.Empty;
        return false;
    }

    public static bool IsDeclaredType(string? word) => TryNormalize(word, out _);

    /// <summary>The kind a type word maps to, or false when the word is not a type.</summary>
    public static bool TryGetKind(string? word, out VariantKind kind)
    {
        foreach (var entry in Map)
        {
            if (string.Equals(entry.Type, word, StringComparison.OrdinalIgnoreCase))
            {
                kind = entry.Kind;
                return true;
            }
        }

        kind = VariantKind.Null;
        return false;
    }

    /// <summary>
    /// The kind of a stored type word. A stored word that is not a type, which only a record written around the check can hold,
    /// maps to text.
    /// </summary>
    public static VariantKind GetStoredKind(string? word) => TryGetKind(word, out var kind) ? kind : VariantKind.String;

    /// <summary>The type word of a kind. No value is not declarable, so asking for its type is an error.</summary>
    public static string GetTypeWord(VariantKind kind)
    {
        foreach (var entry in Map)
        {
            if (entry.Kind == kind)
            {
                return entry.Type;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, "No value has no declared type.");
    }
}
