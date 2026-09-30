using Scaidome.Abstractions;

namespace Scaidome.Abstractions.Tests;

public class NameRulesTests
{
    [Theory]
    [InlineData("Pump_1")]
    [InlineData("Température")]
    [InlineData("Насос")]
    [InlineData("Αντλία")]
    [InlineData("משאבה")]
    [InlineData("مضخة")]
    [InlineData("पंप")]
    [InlineData("ปั๊ม")]
    [InlineData("泵站")]
    [InlineData("ポンプ1号")]
    [InlineData("ポンプ_Line")]
    [InlineData("펌프")]
    [InlineData("펌프站")]
    [InlineData("ㄅㄆ中文")]
    [InlineData("µValue")]
    [InlineData("aː")]
    [InlineData("𠀀𠀁")]
    [InlineData("x́")]
    public void Valid_tag_names(string name)
    {
        NameRules.Check(name, NameForm.TagName).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_names_are_missing(string? name)
    {
        NameRules.Check(name, NameForm.TagName).Should().Be(NameRules.MissingMessage);
        NameRules.CanonicalizeAndCheck(name, out _, NameForm.Loose).Should().Be(NameRules.MissingMessage);
    }

    [Theory]
    [InlineData("Pump-1")]
    [InlineData("Pump 1")]
    [InlineData("Pump.1")]
    [InlineData("Pump$")]
    [InlineData("Pump\t1")]
    [InlineData("Ⅻ")]
    [InlineData("〇")]
    [InlineData("²")]
    public void Characters_outside_the_tag_name_form_are_refused(string name)
    {
        NameRules.Check(name, NameForm.TagName).Should().Be(NameRules.TagNameCharactersMessage);
    }

    [Fact]
    public void The_loose_form_also_allows_hyphens_and_spaces()
    {
        NameRules.Check("Line 1 - North").Should().BeNull();
        NameRules.Check("Line.1").Should().Be(NameRules.LooseCharactersMessage);
    }

    [Theory]
    [InlineData("a​b")]
    [InlineData("a­b")]
    [InlineData("a⁠b")]
    [InlineData("ㅤa")]
    [InlineData("a️")]
    [InlineData("a͏b")]
    [InlineData("a\U000E0041")]
    public void Invisible_and_formatting_characters_are_refused(string name)
    {
        NameRules.Check(name, NameForm.TagName).Should().Be(NameRules.InvisibleCharactersMessage);
    }

    [Theory]
    [InlineData("Բարեւ")]
    [InlineData("გამარჯობა")]
    [InlineData("ⲁⲃ")]
    [InlineData("𐌰𐌱")]
    public void Unsupported_writing_systems_are_refused(string name)
    {
        NameRules.Check(name, NameForm.TagName).Should().Be(NameRules.WritingSystemsMessage);
    }

    [Theory]
    [InlineData("Pumpа")]
    [InlineData("αbc")]
    [InlineData("中文ㅎㅎ가ポ")]
    [InlineData("ㄅㅎ")]
    [InlineData("שלוםhello")]
    public void Mixing_writing_systems_is_refused(string name)
    {
        NameRules.Check(name, NameForm.TagName).Should().Be(NameRules.MixedWritingSystemsMessage);
    }

    [Fact]
    public void A_modifier_letter_counts_as_its_own_writing_system()
    {
        // ʰ is a Latin modifier letter; beside Cyrillic it mixes.
        NameRules.Check("Насосʰ", NameForm.TagName).Should().Be(NameRules.MixedWritingSystemsMessage);
        NameRules.Check("Pumpʰ", NameForm.TagName).Should().BeNull();
    }

    [Fact]
    public void A_character_outside_the_basic_plane_is_one_character()
    {
        NameRules.Check("𝐀𝐁", NameForm.TagName).Should().BeNull();
        NameRules.Check("a\uD800b", NameForm.TagName).Should().Be(NameRules.TagNameCharactersMessage);
    }

    [Fact]
    public void Canonicalising_converts_ideographic_space_composes_and_trims()
    {
        NameRules.CanonicalizeAndCheck("\u3000Café Line\u3000", out var canonical).Should().BeNull();
        canonical.Should().Be("Café Line");
    }

    [Fact]
    public void Text_that_cannot_be_composed_is_refused_with_the_character_message()
    {
        NameRules.Canonicalize("a\uDC00").Should().BeNull();
        NameRules.CanonicalizeAndCheck("a\uDC00", out _, NameForm.TagName).Should().Be(NameRules.TagNameCharactersMessage);
        NameRules.CanonicalizeAndCheck("a\uDC00", out _, NameForm.DottedIdentifier)
            .Should().Be(NameRules.DottedIdentifierCharactersMessage);
    }

    [Theory]
    [InlineData("Area.Limit", null)]
    [InlineData("Unit.Sub.Leaf", null)]
    [InlineData("Area..Limit", NameRules.DottedIdentifierCharactersMessage)]
    [InlineData(".Limit", NameRules.DottedIdentifierCharactersMessage)]
    [InlineData("Limit.", NameRules.DottedIdentifierCharactersMessage)]
    [InlineData("Area-1.Limit", NameRules.DottedIdentifierCharactersMessage)]
    [InlineData("Area.Лимит", NameRules.MixedWritingSystemsMessage)]
    public void Dotted_identifiers(string name, string? refusal)
    {
        NameRules.Check(name, NameForm.DottedIdentifier).Should().Be(refusal);
    }

    [Fact]
    public void An_empty_segment_is_refused_only_after_the_character_rule()
    {
        NameRules.Check("a..b​", NameForm.DottedIdentifier).Should().Be(NameRules.InvisibleCharactersMessage);
    }

    [Fact]
    public void Prose_is_composed_and_trimmed_and_keeps_inner_ideographic_spaces()
    {
        NameRules.CanonicalizeProse("  Café\u3000bar  ").Should().Be("Café\u3000bar");
        NameRules.CanonicalizeProse(null).Should().BeEmpty();
        NameRules.CanonicalizeProse("any $ text!").Should().Be("any $ text!");
    }

    [Fact]
    public void The_comparison_key_folds_case_through_lower_then_upper()
    {
        NameRules.ComparisonKey(" straße ").Should().Be(NameRules.ComparisonKey("STRAẞE"));
        NameRules.ComparisonKey("ẞ").Should().Be(NameRules.ComparisonKey("ß"));
        NameRules.ComparisonKey("Café").Should().Be(NameRules.ComparisonKey("CAFÉ"));
        NameRules.ComparisonKey("pump").Should().Be("PUMP");
        NameRules.NamesEqual("Pump", "PUMP ").Should().BeTrue();
        NameRules.NamesEqual("Pump", "Pumps").Should().BeFalse();
    }

    [Fact]
    public void The_writing_system_table_is_sorted_and_does_not_overlap()
    {
        var ranges = NameRules.WritingSystemRanges;
        for (var i = 0; i < ranges.Length; i++)
        {
            ranges[i].Start.Should().BeLessThanOrEqualTo(ranges[i].End);
            if (i > 0)
            {
                ranges[i].Start.Should().BeGreaterThan(ranges[i - 1].End, $"range {i} starts after range {i - 1}");
            }
        }
    }
}
