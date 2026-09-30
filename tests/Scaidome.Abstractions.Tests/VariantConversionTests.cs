namespace Scaidome.Abstractions.Tests;

public class VariantConversionTests
{
    [Fact]
    public void A_value_already_of_the_kind_is_unchanged()
    {
        var value = Variant.FromColor("#abc");

        VariantConversion.TryConvert(value, VariantKind.Color, out var result).Should().BeTrue();
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(5.0, true, 5)]
    [InlineData(5.5, false, 0)]
    [InlineData(3_000_000_000.0, false, 0)]
    public void Integer_from_a_number_needs_no_fraction_and_to_fit(double number, bool converts, int expected)
    {
        VariantConversion.TryConvert(Variant.FromDouble(number), VariantKind.Int, out var result).Should().Be(converts);
        if (converts)
        {
            result.Should().Be(Variant.FromInt(expected));
        }
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData(" -3 ", true)]
    [InlineData("4.0", false)]
    [InlineData("1e3", false)]
    [InlineData("x", false)]
    public void Integer_from_text(string text, bool converts)
    {
        VariantConversion.TryConvert(Variant.FromString(text), VariantKind.Int, out _).Should().Be(converts);
    }

    [Fact]
    public void Booleans_convert_to_numbers_as_one_and_zero()
    {
        VariantConversion.Convert(Variant.FromBool(true), VariantKind.Int).Should().Be(Variant.FromInt(1));
        VariantConversion.Convert(false, VariantKind.Double).Should().Be(Variant.FromDouble(0));
        VariantConversion.Convert(true, VariantKind.Epoch).Should().Be(Variant.FromEpoch(1));
    }

    [Theory]
    [InlineData("2.5", true)]
    [InlineData("-1E3", true)]
    [InlineData("NaN", false)]
    [InlineData("Infinity", false)]
    [InlineData("2,5", false)]
    public void Number_from_text_in_invariant_form(string text, bool converts)
    {
        VariantConversion.TryConvert(text, VariantKind.Double, out _).Should().Be(converts);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("FALSE", false)]
    [InlineData("0", false)]
    [InlineData("0.5", true)]
    public void Boolean_from_text(string text, bool expected)
    {
        VariantConversion.Convert(text, VariantKind.Bool).Should().Be(Variant.FromBool(expected));
    }

    [Fact]
    public void Boolean_from_a_number_is_true_when_not_zero()
    {
        VariantConversion.Convert(Variant.FromDouble(-0.1), VariantKind.Bool).Should().Be(Variant.FromBool(true));
        VariantConversion.Convert(0, VariantKind.Bool).Should().Be(Variant.FromBool(false));
        VariantConversion.TryConvert("yes", VariantKind.Bool, out _).Should().BeFalse();
    }

    [Fact]
    public void Text_kinds_convert_among_themselves_keeping_the_text()
    {
        VariantConversion.Convert(Variant.FromColor("red"), VariantKind.String).Should().Be(Variant.FromString("red"));
        VariantConversion.Convert(Variant.FromJson("{}"), VariantKind.Matrix).Should().Be(Variant.FromMatrix("{}"));
        VariantConversion.TryConvert(Variant.FromInt(1), VariantKind.String, out _).Should().BeFalse();
    }

    [Fact]
    public void Json_needs_well_formed_text()
    {
        VariantConversion.Convert("[1]", VariantKind.Json).Should().Be(Variant.FromJson("[1]"));
        VariantConversion.TryConvert("not json", VariantKind.Json, out _).Should().BeFalse();
        VariantConversion.TryConvert(3, VariantKind.Json, out _).Should().BeFalse();
    }

    [Fact]
    public void Date_and_time_converts_from_a_date_and_time_only()
    {
        var instant = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        VariantConversion.Convert(instant, VariantKind.DateTime).Should().Be(Variant.FromDateTime(instant));
        VariantConversion.TryConvert(Variant.FromEpoch(1_700_000_000), VariantKind.DateTime, out _).Should().BeFalse();
        VariantConversion.TryConvert("2026-01-01T00:00:00Z", VariantKind.DateTime, out _).Should().BeFalse();
    }

    [Fact]
    public void Every_platform_number_type_converts()
    {
        object[] numbers = [(byte)1, (sbyte)1, (short)1, (ushort)1, 1, 1u, 1L, 1ul, 1f, 1d, 1m];
        foreach (var number in numbers)
        {
            VariantConversion.Convert(number, VariantKind.Int).Should().Be(Variant.FromInt(1));
        }
    }

    [Fact]
    public void Plain_values_become_values_of_their_kind()
    {
        VariantConversion.TryFromPlain(null, out var none).Should().BeTrue();
        none.IsNull.Should().BeTrue();
        VariantConversion.TryFromPlain(4L, out var whole).Should().BeTrue();
        whole.Should().Be(Variant.FromInt(4));
        VariantConversion.TryFromPlain(long.MaxValue, out var large).Should().BeTrue();
        large.Kind.Should().Be(VariantKind.Double);
        VariantConversion.TryFromPlain(2.5f, out var single).Should().BeTrue();
        single.Should().Be(Variant.FromDouble(2.5));
        VariantConversion.TryFromPlain(double.NaN, out _).Should().BeFalse();
        VariantConversion.TryFromPlain("text", out _).Should().BeFalse("text could mean any of four kinds");
        VariantConversion.TryFromPlain(new object(), out _).Should().BeFalse();
    }

    [Fact]
    public void Unknown_plain_types_do_not_convert()
    {
        VariantConversion.TryConvert(new object(), VariantKind.String, out _).Should().BeFalse();
        FluentActions.Invoking(() => VariantConversion.Convert(Guid.Empty, VariantKind.String)).Should().Throw<FormatException>();
    }

    [Fact]
    public void Plain_values_as_text_use_the_invariant_culture()
    {
        VariantConversion.ToInvariantText(null).Should().BeNull();
        VariantConversion.ToInvariantText(1.5).Should().Be("1.5");
        VariantConversion.ToInvariantText(1234567m).Should().Be("1234567");
        VariantConversion.ToInvariantText(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified))
            .Should().Be("2026-01-01T00:00:00.0000000Z");
        VariantConversion.ToInvariantText(new DateTimeOffset(2026, 1, 1, 2, 0, 0, TimeSpan.FromHours(2)))
            .Should().Be("2026-01-01T00:00:00.0000000Z");
    }
}
