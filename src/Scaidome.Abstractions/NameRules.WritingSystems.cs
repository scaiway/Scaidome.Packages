namespace Scaidome.Abstractions;

public static partial class NameRules
{
    /// <summary>The writing systems a name may use. Common marks a character Unicode assigns to no single writing system.</summary>
    [Flags]
    internal enum WritingSystem
    {
        None = 0,
        Latin = 1 << 0,
        Greek = 1 << 1,
        Cyrillic = 1 << 2,
        Hebrew = 1 << 3,
        Arabic = 1 << 4,
        Devanagari = 1 << 5,
        Thai = 1 << 6,
        Han = 1 << 7,
        Hiragana = 1 << 8,
        Katakana = 1 << 9,
        Hangul = 1 << 10,
        Bopomofo = 1 << 11,
        Common = 1 << 20,
        Unsupported = 1 << 21,
    }

    /// <summary>
    /// The Unicode ranges of the letters and digits of the supported writing systems, and of the letters and digits that belong
    /// to none (Common). Sorted by start and not overlapping. A letter or digit in no range belongs to a writing system the
    /// rules do not support. Internal for the test that proves the table sorted.
    /// </summary>
    internal static readonly (int Start, int End, WritingSystem System)[] WritingSystemRanges =
    [
        (0x0030, 0x0039, WritingSystem.Common),
        (0x0041, 0x005A, WritingSystem.Latin),
        (0x0061, 0x007A, WritingSystem.Latin),
        (0x00AA, 0x00AA, WritingSystem.Latin),
        (0x00B5, 0x00B5, WritingSystem.Common),
        (0x00BA, 0x00BA, WritingSystem.Latin),
        (0x00C0, 0x00D6, WritingSystem.Latin),
        (0x00D8, 0x00F6, WritingSystem.Latin),
        (0x00F8, 0x02B8, WritingSystem.Latin),
        (0x02B9, 0x02DF, WritingSystem.Common),
        (0x02E0, 0x02E4, WritingSystem.Latin),
        (0x02E5, 0x02E9, WritingSystem.Common),
        (0x02EA, 0x02EB, WritingSystem.Bopomofo),
        (0x02EC, 0x02FF, WritingSystem.Common),
        (0x0370, 0x0373, WritingSystem.Greek),
        (0x0374, 0x0374, WritingSystem.Common),
        (0x0375, 0x0377, WritingSystem.Greek),
        (0x037A, 0x037D, WritingSystem.Greek),
        (0x037E, 0x037E, WritingSystem.Common),
        (0x037F, 0x0384, WritingSystem.Greek),
        (0x0385, 0x0385, WritingSystem.Common),
        (0x0386, 0x0386, WritingSystem.Greek),
        (0x0387, 0x0387, WritingSystem.Common),
        (0x0388, 0x03E1, WritingSystem.Greek),
        (0x03F0, 0x03FF, WritingSystem.Greek),
        (0x0400, 0x052F, WritingSystem.Cyrillic),
        (0x0591, 0x05C7, WritingSystem.Hebrew),
        (0x05D0, 0x05F4, WritingSystem.Hebrew),
        (0x0600, 0x0604, WritingSystem.Arabic),
        (0x0605, 0x0605, WritingSystem.Common),
        (0x0606, 0x060B, WritingSystem.Arabic),
        (0x060C, 0x060C, WritingSystem.Common),
        (0x060D, 0x061A, WritingSystem.Arabic),
        (0x061B, 0x061B, WritingSystem.Common),
        (0x061C, 0x061E, WritingSystem.Arabic),
        (0x061F, 0x061F, WritingSystem.Common),
        (0x0620, 0x063F, WritingSystem.Arabic),
        (0x0640, 0x0640, WritingSystem.Common),
        (0x0641, 0x06DC, WritingSystem.Arabic),
        (0x06DD, 0x06DD, WritingSystem.Common),
        (0x06DE, 0x06FF, WritingSystem.Arabic),
        (0x0750, 0x077F, WritingSystem.Arabic),
        (0x0870, 0x08FF, WritingSystem.Arabic),
        (0x0900, 0x0963, WritingSystem.Devanagari),
        (0x0964, 0x0965, WritingSystem.Common),
        (0x0966, 0x097F, WritingSystem.Devanagari),
        (0x0E01, 0x0E3A, WritingSystem.Thai),
        (0x0E3F, 0x0E3F, WritingSystem.Common),
        (0x0E40, 0x0E5B, WritingSystem.Thai),
        (0x1100, 0x11FF, WritingSystem.Hangul),
        (0x1C80, 0x1C8A, WritingSystem.Cyrillic),
        (0x1CE9, 0x1CEC, WritingSystem.Common),
        (0x1CEE, 0x1CF3, WritingSystem.Common),
        (0x1CF5, 0x1CF6, WritingSystem.Common),
        (0x1CFA, 0x1CFA, WritingSystem.Common),
        (0x1D00, 0x1D25, WritingSystem.Latin),
        (0x1D26, 0x1D2A, WritingSystem.Greek),
        (0x1D2B, 0x1D2B, WritingSystem.Cyrillic),
        (0x1D2C, 0x1D5C, WritingSystem.Latin),
        (0x1D5D, 0x1D61, WritingSystem.Greek),
        (0x1D62, 0x1D65, WritingSystem.Latin),
        (0x1D66, 0x1D6A, WritingSystem.Greek),
        (0x1D6B, 0x1D77, WritingSystem.Latin),
        (0x1D78, 0x1D78, WritingSystem.Cyrillic),
        (0x1D79, 0x1DBE, WritingSystem.Latin),
        (0x1DBF, 0x1DBF, WritingSystem.Greek),
        (0x1E00, 0x1EFF, WritingSystem.Latin),
        (0x1F00, 0x1FFE, WritingSystem.Greek),
        (0x2071, 0x2071, WritingSystem.Latin),
        (0x207F, 0x207F, WritingSystem.Latin),
        (0x2090, 0x209C, WritingSystem.Latin),
        (0x2102, 0x2102, WritingSystem.Common),
        (0x2107, 0x2107, WritingSystem.Common),
        (0x210A, 0x2113, WritingSystem.Common),
        (0x2115, 0x2115, WritingSystem.Common),
        (0x2119, 0x211D, WritingSystem.Common),
        (0x2124, 0x2124, WritingSystem.Common),
        (0x2126, 0x2126, WritingSystem.Greek),
        (0x2128, 0x2128, WritingSystem.Common),
        (0x212A, 0x212B, WritingSystem.Latin),
        (0x212C, 0x212D, WritingSystem.Common),
        (0x212F, 0x2131, WritingSystem.Common),
        (0x2132, 0x2132, WritingSystem.Latin),
        (0x2133, 0x2139, WritingSystem.Common),
        (0x213C, 0x213F, WritingSystem.Common),
        (0x2145, 0x2149, WritingSystem.Common),
        (0x214E, 0x214E, WritingSystem.Latin),
        (0x2160, 0x2188, WritingSystem.Latin),
        (0x2C60, 0x2C7F, WritingSystem.Latin),
        (0x2DE0, 0x2DFF, WritingSystem.Cyrillic),
        (0x2E80, 0x2E99, WritingSystem.Han),
        (0x2E9B, 0x2EF3, WritingSystem.Han),
        (0x2F00, 0x2FD5, WritingSystem.Han),
        (0x3005, 0x3005, WritingSystem.Han),
        (0x3006, 0x3006, WritingSystem.Common),
        (0x3007, 0x3007, WritingSystem.Han),
        (0x3021, 0x3029, WritingSystem.Han),
        (0x302E, 0x302F, WritingSystem.Hangul),
        (0x3031, 0x3035, WritingSystem.Common),
        (0x3038, 0x303B, WritingSystem.Han),
        (0x303C, 0x303C, WritingSystem.Common),
        (0x3041, 0x3096, WritingSystem.Hiragana),
        (0x309D, 0x309F, WritingSystem.Hiragana),
        (0x30A1, 0x30FA, WritingSystem.Katakana),
        (0x30FC, 0x30FC, WritingSystem.Common),
        (0x30FD, 0x30FF, WritingSystem.Katakana),
        (0x3105, 0x312F, WritingSystem.Bopomofo),
        (0x3131, 0x318E, WritingSystem.Hangul),
        (0x31A0, 0x31BF, WritingSystem.Bopomofo),
        (0x31F0, 0x31FF, WritingSystem.Katakana),
        (0x3400, 0x4DBF, WritingSystem.Han),
        (0x4E00, 0x9FFF, WritingSystem.Han),
        (0xA640, 0xA69F, WritingSystem.Cyrillic),
        (0xA717, 0xA71F, WritingSystem.Common),
        (0xA722, 0xA787, WritingSystem.Latin),
        (0xA788, 0xA788, WritingSystem.Common),
        (0xA78B, 0xA7FF, WritingSystem.Latin),
        (0xA8E0, 0xA8FF, WritingSystem.Devanagari),
        (0xA960, 0xA97C, WritingSystem.Hangul),
        (0xA9CF, 0xA9CF, WritingSystem.Common),
        (0xAB30, 0xAB5A, WritingSystem.Latin),
        (0xAB5C, 0xAB64, WritingSystem.Latin),
        (0xAB65, 0xAB65, WritingSystem.Greek),
        (0xAB66, 0xAB69, WritingSystem.Latin),
        (0xAC00, 0xD7A3, WritingSystem.Hangul),
        (0xD7B0, 0xD7FB, WritingSystem.Hangul),
        (0xF900, 0xFAD9, WritingSystem.Han),
        (0xFB00, 0xFB06, WritingSystem.Latin),
        (0xFB1D, 0xFB4F, WritingSystem.Hebrew),
        (0xFB50, 0xFDFF, WritingSystem.Arabic),
        (0xFE2E, 0xFE2F, WritingSystem.Cyrillic),
        (0xFE70, 0xFEFC, WritingSystem.Arabic),
        (0xFF10, 0xFF19, WritingSystem.Common),
        (0xFF21, 0xFF3A, WritingSystem.Latin),
        (0xFF41, 0xFF5A, WritingSystem.Latin),
        (0xFF66, 0xFF6F, WritingSystem.Katakana),
        (0xFF70, 0xFF70, WritingSystem.Common),
        (0xFF71, 0xFF9D, WritingSystem.Katakana),
        (0xFF9E, 0xFF9F, WritingSystem.Common),
        (0xFFA0, 0xFFDC, WritingSystem.Hangul),
        (0x10140, 0x1018E, WritingSystem.Greek),
        (0x10780, 0x107BA, WritingSystem.Latin),
        (0x10E60, 0x10E7E, WritingSystem.Arabic),
        (0x16FE2, 0x16FE3, WritingSystem.Han),
        (0x16FF0, 0x16FF1, WritingSystem.Han),
        (0x1AFF0, 0x1AFFE, WritingSystem.Katakana),
        (0x1B000, 0x1B000, WritingSystem.Katakana),
        (0x1B001, 0x1B11F, WritingSystem.Hiragana),
        (0x1B120, 0x1B122, WritingSystem.Katakana),
        (0x1B132, 0x1B132, WritingSystem.Hiragana),
        (0x1B150, 0x1B152, WritingSystem.Hiragana),
        (0x1B155, 0x1B155, WritingSystem.Katakana),
        (0x1B164, 0x1B167, WritingSystem.Katakana),
        (0x1D400, 0x1D7FF, WritingSystem.Common),
        (0x1DF00, 0x1DF1E, WritingSystem.Latin),
        (0x1E030, 0x1E08F, WritingSystem.Cyrillic),
        (0x1EE00, 0x1EEFF, WritingSystem.Arabic),
        (0x1F200, 0x1F200, WritingSystem.Hiragana),
        (0x1FBF0, 0x1FBF9, WritingSystem.Common),
        (0x20000, 0x2A6DF, WritingSystem.Han),
        (0x2A700, 0x2EBEF, WritingSystem.Han),
        (0x2EBF0, 0x2EE5F, WritingSystem.Han),
        (0x2F800, 0x2FA1F, WritingSystem.Han),
        (0x30000, 0x323AF, WritingSystem.Han),
    ];

    /// <summary>The writing system of a letter or digit, by binary search of the range table.</summary>
    internal static WritingSystem GetWritingSystem(int codePoint)
    {
        var low = 0;
        var high = WritingSystemRanges.Length - 1;
        while (low <= high)
        {
            var middle = (low + high) >>> 1;
            var range = WritingSystemRanges[middle];
            if (codePoint < range.Start)
            {
                high = middle - 1;
            }
            else if (codePoint > range.End)
            {
                low = middle + 1;
            }
            else
            {
                return range.System;
            }
        }

        return WritingSystem.Unsupported;
    }
}
