namespace Scaidome.Abstractions.Tests;

public class VariantTextFormTests
{
    public static TheoryData<Variant> RoundTripValues() => new()
    {
        Variant.Null,
        Variant.FromInt(int.MinValue),
        Variant.FromDouble(0.1 + 0.2),
        Variant.FromDouble(double.MaxValue),
        Variant.FromEpoch(1_726_000_000_123.456),
        Variant.FromBool(true),
        Variant.FromBool(false),
        Variant.FromString("text with ünïcödé"),
        Variant.FromString(string.Empty),
        Variant.FromJson("[1,{\"a\":null}]"),
        Variant.FromColor("nonsense"),
        Variant.FromMatrix("1 0 0 1 0 0"),
        Variant.FromDateTime(new DateTime(2026, 9, 3, 14, 22, 7, 123, DateTimeKind.Utc).AddTicks(4567)),
    };

    [Theory]
    [MemberData(nameof(RoundTripValues))]
    public void A_value_round_trips_with_its_kind(Variant value)
    {
        var (text, kind) = VariantTextForm.Write(value);

        VariantTextForm.TryRead(text, kind, out var read).Should().BeTrue();
        read.Should().Be(value);
    }

    [Fact]
    public void Forms_are_as_specified()
    {
        VariantTextForm.Write(Variant.FromBool(true)).Should().Be(("True", "Bool"));
        VariantTextForm.Write(Variant.Null).Should().Be((string.Empty, "Null"));
        VariantTextForm.Write(Variant.FromDouble(2.5)).Should().Be(("2.5", "Double"));
        VariantTextForm.Write(Variant.FromDateTime(new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc)))
            .Should().Be(("2026-09-03T00:00:00.0000000Z", "DateTime"));
    }

    [Theory]
    [InlineData("true", "bool")]
    [InlineData("FALSE", "BOOL")]
    public void Booleans_and_kind_names_read_without_regard_to_case(string text, string kind)
    {
        VariantTextForm.TryRead(text, kind, out var value).Should().BeTrue();
        value.Kind.Should().Be(VariantKind.Bool);
    }

    [Theory]
    [InlineData("1", "Unknown")]
    [InlineData("1", "1")]
    [InlineData("1", "Int, Double")]
    [InlineData("1", null)]
    [InlineData("abc", "Int")]
    [InlineData("1.5", "Int")]
    [InlineData("abc", "Double")]
    [InlineData("NaN", "Double")]
    [InlineData("Infinity", "Epoch")]
    [InlineData("1e999", "Double")]
    [InlineData("yes", "Bool")]
    [InlineData("not a date", "DateTime")]
    [InlineData("{", "Json")]
    [InlineData("", "Json")]
    public void Reading_fails_for_what_cannot_be_read(string text, string? kind)
    {
        VariantTextForm.TryRead(text, kind, out _).Should().BeFalse();
    }

    [Fact]
    public void A_date_with_an_offset_is_converted_and_one_without_is_utc()
    {
        VariantTextForm.TryRead("2026-09-03T14:00:00+02:00", "DateTime", out var withOffset).Should().BeTrue();
        withOffset.AsDateTime().Should().Be(new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc));

        VariantTextForm.TryRead("2026-09-03 14:00:00", "DateTime", out var withoutOffset).Should().BeTrue();
        withoutOffset.AsDateTime().Should().Be(new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc));
        withoutOffset.AsDateTime().Kind.Should().Be(DateTimeKind.Utc);
    }
}
