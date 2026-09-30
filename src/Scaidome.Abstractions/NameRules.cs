using System.Globalization;
using System.Numerics;
using System.Text;

namespace Scaidome.Abstractions;

/// <summary>
/// The one implementation of the name and text rules every model uses. A check returns nothing when the name is valid and the
/// refusal message otherwise.
/// </summary>
public static partial class NameRules
{
    public const string MissingMessage = "Name is required.";
    public const string LooseCharactersMessage = "Name may only contain letters, digits, underscores, hyphens, and spaces.";
    public const string TagNameCharactersMessage = "Name may only contain letters, digits, and underscores.";
    public const string DottedIdentifierCharactersMessage =
        "Name may only contain letters, digits and underscores, in dot-separated parts.";
    public const string InvisibleCharactersMessage = "Name may not contain invisible or formatting characters.";
    public const string WritingSystemsMessage =
        "Name may only use the Latin, Greek, Cyrillic, Hebrew, Arabic, Devanagari, Thai, Chinese, Japanese and Korean writing systems.";
    public const string MixedWritingSystemsMessage = "Name may not mix letters from different writing systems.";

    private const char IdeographicSpace = '\u3000';

    /// <summary>
    /// Canonicalises a name: an ideographic space becomes a plain space, then Unicode composition, then trimming, in that
    /// order. Returns null when the text is not well-formed Unicode and cannot be composed.
    /// </summary>
    public static string? Canonicalize(string? name)
    {
        if (name is null)
        {
            return string.Empty;
        }

        var composed = TryCompose(name.Replace(IdeographicSpace, ' '));
        return composed?.Trim();
    }

    /// <summary>
    /// Canonicalises and checks a name in one call. Returns the refusal message, or null when the name is valid, and hands back
    /// the form to store.
    /// </summary>
    public static string? CanonicalizeAndCheck(string? name, out string canonical, NameForm form = NameForm.Loose)
    {
        var result = Canonicalize(name);
        if (result is null)
        {
            // Text that cannot be composed is refused with the character rule's message.
            canonical = name ?? string.Empty;
            return CharactersMessage(form);
        }

        canonical = result;
        return Check(result, form);
    }

    /// <summary>Checks a name that is already canonical.</summary>
    public static string? Check(string? name, NameForm form = NameForm.Loose)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return MissingMessage;
        }

        // The name is scanned once, by code point, with no regular expression: a regular expression sees a character outside
        // the basic plane as two halves. Every failure is noted during the scan and reported in a fixed order of precedence.
        var invisible = false;
        var badCharacter = false;
        var unsupported = false;
        var systems = WritingSystem.None;

        for (var i = 0; i < name.Length;)
        {
            int codePoint;
            if (char.IsHighSurrogate(name[i]) && i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
            {
                codePoint = char.ConvertToUtf32(name[i], name[i + 1]);
                i += 2;
            }
            else if (char.IsSurrogate(name[i]))
            {
                badCharacter = true;
                i++;
                continue;
            }
            else
            {
                codePoint = name[i];
                i++;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(codePoint);
            if (IsInvisible(codePoint, category))
            {
                invisible = true;
                continue;
            }

            switch (category)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    var system = GetWritingSystem(codePoint);
                    if (system == WritingSystem.Unsupported)
                    {
                        unsupported = true;
                    }
                    else if (system != WritingSystem.Common)
                    {
                        systems |= system;
                    }

                    break;
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.EnclosingMark:
                    // A combining mark belongs to no writing system: it takes the one of the letter it follows, which the
                    // scan has already counted.
                    break;
                default:
                    if (!IsAllowedSeparator(codePoint, form))
                    {
                        badCharacter = true;
                    }

                    break;
            }
        }

        if (invisible)
        {
            return InvisibleCharactersMessage;
        }

        if (badCharacter)
        {
            return CharactersMessage(form);
        }

        if (unsupported)
        {
            return WritingSystemsMessage;
        }

        if (!IsAllowedCombination(systems))
        {
            return MixedWritingSystemsMessage;
        }

        if (form == NameForm.DottedIdentifier && name.Split('.').Any(segment => segment.Length == 0))
        {
            // An empty segment is refused only after the character rule has passed.
            return DottedIdentifierCharactersMessage;
        }

        return null;
    }

    /// <summary>
    /// Prose, such as descriptions, alarm labels and engineering units: composed and trimmed, with no character rule. An
    /// ideographic space inside the text is kept.
    /// </summary>
    public static string CanonicalizeProse(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return (TryCompose(text) ?? text).Trim();
    }

    /// <summary>
    /// The key names are compared through for uniqueness: composed and trimmed, then lower-cased and then upper-cased, both in
    /// the invariant culture. Lower-casing first brings together capitals with no upper-case mapping of their own, such as ẞ
    /// and ß. Names that compare equal have the same key.
    /// </summary>
    public static string ComparisonKey(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var composed = (TryCompose(name) ?? name).Trim();
        return composed.ToLowerInvariant().ToUpperInvariant();
    }

    /// <summary>True when two names are equal in their comparison keys.</summary>
    public static bool NamesEqual(string? left, string? right) =>
        string.Equals(ComparisonKey(left), ComparisonKey(right), StringComparison.Ordinal);

    private static string CharactersMessage(NameForm form) => form switch
    {
        NameForm.TagName => TagNameCharactersMessage,
        NameForm.DottedIdentifier => DottedIdentifierCharactersMessage,
        _ => LooseCharactersMessage,
    };

    private static bool IsAllowedSeparator(int codePoint, NameForm form) => codePoint switch
    {
        '_' => true,
        '-' or ' ' => form == NameForm.Loose,
        '.' => form == NameForm.DottedIdentifier,
        _ => false,
    };

    private static string? TryCompose(string text)
    {
        try
        {
            return text.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Formatting characters, and the default-ignorable letters and marks that display as nothing, such as the Hangul fillers
    /// and the variation selectors. A name containing one could look identical to another name.
    /// </summary>
    private static bool IsInvisible(int codePoint, UnicodeCategory category) =>
        category == UnicodeCategory.Format
        || codePoint is 0x034F or 0x115F or 0x1160 or 0x17B4 or 0x17B5 or 0x3164 or 0xFFA0
        || codePoint is >= 0x180B and <= 0x180F
        || codePoint is >= 0xFE00 and <= 0xFE0F
        || codePoint is >= 0xE0000 and <= 0xE0FFF;

    private static bool IsAllowedCombination(WritingSystem systems)
    {
        if (BitOperations.PopCount((uint)systems) <= 1)
        {
            return true;
        }

        // The combinations CJK text uses, each of which may also contain Latin.
        const WritingSystem japanese = WritingSystem.Han | WritingSystem.Hiragana | WritingSystem.Katakana | WritingSystem.Latin;
        const WritingSystem korean = WritingSystem.Han | WritingSystem.Hangul | WritingSystem.Latin;
        const WritingSystem chinese = WritingSystem.Han | WritingSystem.Bopomofo | WritingSystem.Latin;
        return (systems & ~japanese) == 0 || (systems & ~korean) == 0 || (systems & ~chinese) == 0;
    }
}
