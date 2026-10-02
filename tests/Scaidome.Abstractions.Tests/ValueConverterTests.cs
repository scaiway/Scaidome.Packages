namespace Scaidome.Abstractions.Tests;

public class ValueConverterTests
{
    public static TheoryData<object, int> WholeNumbers => new()
    {
        { (byte)7, 7 },
        { (sbyte)-7, -7 },
        { (short)-300, -300 },
        { (ushort)60000, 60000 },
        { 42, 42 },
        { 42u, 42 },
        { -42L, -42 },
        { 42UL, 42 },
        { 3.9f, 3 },
        { -3.9, -3 },
        { 3.9m, 3 },
        { "17", 17 },
        { true, 1 },
    };

    [Theory]
    [MemberData(nameof(WholeNumbers))]
    public void Int_from_any_number_truncates_toward_zero(object value, int expected)
    {
        ValueConverter.TryConvertToInt(value, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    public static TheoryData<object?> NotInts => new()
    {
        null,
        uint.MaxValue,
        long.MaxValue,
        long.MinValue,
        ulong.MaxValue,
        3e10,
        double.NaN,
        double.PositiveInfinity,
        "4.5",
        "true",
        "x",
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        new object(),
    };

    [Theory]
    [MemberData(nameof(NotInts))]
    public void Int_refuses_what_does_not_fit_rather_than_wrapping(object? value)
    {
        ValueConverter.TryConvertToInt(value, out var result).Should().BeFalse();
        result.Should().Be(0);
    }

    public static TheoryData<object, double> Numbers => new()
    {
        { (byte)7, 7 },
        { -42, -42 },
        { ulong.MaxValue, ulong.MaxValue },
        { 1.5f, 1.5 },
        { 2.25, 2.25 },
        { 2.25m, 2.25 },
        { "1,234.5", 1234.5 },
        { "1e3", 1000 },
        { false, 0 },
    };

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Double_from_any_number_or_number_text(object value, double expected)
    {
        ValueConverter.TryConvertToDouble(value, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Double_keeps_non_finite_numbers_for_the_caller_to_judge(double value)
    {
        ValueConverter.TryConvertToDouble(value, out var result).Should().BeTrue();
        result.Should().Be(value);
    }

    [Fact]
    public void Double_reads_non_finite_invariant_text()
    {
        ValueConverter.TryConvertToDouble("NaN", out var result).Should().BeTrue();
        double.IsNaN(result).Should().BeTrue();
    }

    [Fact]
    public void Double_reads_text_in_the_invariant_culture_whatever_the_current_one()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("da-DK");
        try
        {
            ValueConverter.TryConvertToDouble("3.14", out var result).Should().BeTrue();
            result.Should().Be(3.14);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    public static TheoryData<object?> NotDoubles => new()
    {
        null,
        "x",
        "",
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        new object(),
    };

    [Theory]
    [MemberData(nameof(NotDoubles))]
    public void Double_refuses_what_is_not_a_number(object? value)
    {
        ValueConverter.TryConvertToDouble(value, out var result).Should().BeFalse();
        result.Should().Be(0);
    }
}
