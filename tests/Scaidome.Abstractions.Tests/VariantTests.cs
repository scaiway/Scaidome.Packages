namespace Scaidome.Abstractions.Tests;

public class VariantTests
{
    [Fact]
    public void Default_is_no_value()
    {
        var value = default(Variant);

        value.Kind.Should().Be(VariantKind.Null);
        value.IsNull.Should().BeTrue();
        value.Should().Be(Variant.Null);
        value.ToString().Should().Be("null");
    }

    [Fact]
    public void Kinds_are_declared_in_specification_order()
    {
        Enum.GetNames<VariantKind>().Should().Equal(
            "Null", "Int", "Double", "Bool", "Epoch", "String", "Json", "Color", "Matrix", "DateTime");
    }

    [Fact]
    public void Plain_values_become_their_own_kind_without_naming_it()
    {
        Variant i = 5;
        Variant d = 5.5;
        Variant b = true;
        Variant t = new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc);

        i.Kind.Should().Be(VariantKind.Int);
        d.Kind.Should().Be(VariantKind.Double);
        b.Kind.Should().Be(VariantKind.Bool);
        t.Kind.Should().Be(VariantKind.DateTime);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_number_or_epoch_is_never_NaN_or_infinite(double number)
    {
        FluentActions.Invoking(() => Variant.FromDouble(number)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => Variant.FromEpoch(number)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("{} {}")]
    [InlineData("{'a':1}")]
    public void A_json_value_holds_exactly_one_well_formed_value(string text)
    {
        FluentActions.Invoking(() => Variant.FromJson(text)).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[1,2]")]
    [InlineData("3")]
    [InlineData("\"text\"")]
    [InlineData("null")]
    public void A_json_value_may_have_any_root(string text)
    {
        Variant.FromJson(text).AsText().Should().Be(text);
    }

    [Fact]
    public void A_kind_stored_as_text_is_built_by_naming_it()
    {
        FluentActions.Invoking(() => Variant.FromText(VariantKind.Int, "5")).Should().Throw<ArgumentException>();
        Variant.FromText(VariantKind.Color, "not a colour").AsText().Should().Be("not a colour");
        Variant.FromText(VariantKind.Matrix, string.Empty).AsText().Should().BeEmpty();
    }

    [Fact]
    public void A_date_and_time_with_an_offset_is_converted_to_utc_directly()
    {
        var value = Variant.FromDateTime(new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)));

        value.AsDateTime().Should().Be(new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc));
        value.AsDateTime().Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void A_date_and_time_without_a_kind_is_taken_to_be_utc()
    {
        var value = Variant.FromDateTime(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified));

        value.AsDateTime().Should().Be(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        value.AsDateTime().Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void A_local_date_and_time_is_converted()
    {
        var local = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);

        Variant.FromDateTime(local).AsDateTime().Should().Be(local.ToUniversalTime());
    }

    [Fact]
    public void Reading_within_storage_is_allowed()
    {
        Variant.FromEpoch(12.5).AsDouble().Should().Be(12.5);
        Variant.FromColor("#fff").AsText().Should().Be("#fff");
        Variant.FromInt(1).AsBool().Should().BeTrue();
        Variant.FromInt(0).AsBool().Should().BeFalse();
        Variant.FromBool(true).AsDouble().Should().Be(1);
        Variant.FromDouble(4).AsInt().Should().Be(4);
    }

    [Fact]
    public void Reading_across_storage_fails()
    {
        FluentActions.Invoking(() => Variant.FromString("1").AsDouble()).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Variant.FromInt(1).AsText()).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Variant.FromInt(1).AsDateTime()).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Variant.Null.AsBool()).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Variant.FromDouble(1.5).AsInt()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Storage_questions_answer_by_kind()
    {
        Variant.FromEpoch(1).HasNumber.Should().BeTrue();
        Variant.FromMatrix("m").HasText.Should().BeTrue();
        Variant.FromDateTime(DateTime.UtcNow).HasDateTime.Should().BeTrue();
        Variant.Null.Storage.Should().Be(VariantStorage.None);
    }

    [Fact]
    public void Values_are_equal_only_when_kind_and_content_are_equal()
    {
        Variant.FromInt(1).Should().NotBe(Variant.FromDouble(1));
        Variant.FromColor("#fff").Should().NotBe(Variant.FromString("#fff"));
        Variant.FromInt(1).Should().Be(Variant.FromInt(1));
        (Variant.FromString("a") == Variant.FromString("a")).Should().BeTrue();
        (Variant.FromString("a") != Variant.FromString("A")).Should().BeTrue();
        Variant.FromInt(3).GetHashCode().Should().Be(Variant.FromInt(3).GetHashCode());
    }

    [Fact]
    public void Compared_with_plain_values_a_value_compares_by_storage()
    {
        Variant.FromInt(1).Equals(1.0).Should().BeTrue();
        Variant.FromDouble(1.5).Equals(1).Should().BeFalse("a number is never truncated to match");
        Variant.FromDouble(2).Equals(2L).Should().BeTrue();
        Variant.FromEpoch(2.5).Equals(2.5f).Should().BeTrue();
        Variant.FromDouble(3).Equals(true).Should().BeTrue();
        Variant.FromInt(0).Equals(false).Should().BeTrue();
        Variant.FromColor("#fff").Equals("#fff").Should().BeTrue();
        Variant.FromInt(1).Equals("1").Should().BeFalse();
        var instant = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Variant.FromDateTime(instant).Equals(instant).Should().BeTrue();
        Variant.FromString("x").Equals(new object()).Should().BeFalse();
    }

    [Fact]
    public void Diagnostic_text_is_the_content_in_the_invariant_culture()
    {
        Variant.FromDouble(1234.5).ToString().Should().Be("1234.5");
        Variant.FromBool(true).ToString().Should().Be("true");
        Variant.FromBool(false).ToString().Should().Be("false");
        Variant.FromInt(-7).ToString().Should().Be("-7");
        Variant.FromJson("{\"a\":1}").ToString().Should().Be("{\"a\":1}");
        Variant.FromDateTime(new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc)).ToString()
            .Should().Be("2026-09-03T14:22:07.0000000Z");
    }

    [Fact]
    public void Diagnostic_text_does_not_follow_the_machine_culture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("da-DK");
            Variant.FromDouble(0.5).ToString().Should().Be("0.5");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(VariantKind.Null, "null")]
    [InlineData(VariantKind.Int, "0")]
    [InlineData(VariantKind.Double, "0")]
    [InlineData(VariantKind.Epoch, "0")]
    [InlineData(VariantKind.Bool, "false")]
    [InlineData(VariantKind.String, "")]
    [InlineData(VariantKind.Color, "")]
    [InlineData(VariantKind.Matrix, "")]
    [InlineData(VariantKind.Json, "{}")]
    [InlineData(VariantKind.DateTime, "0001-01-01T00:00:00.0000000Z")]
    public void Zero_of_each_kind(VariantKind kind, string text)
    {
        var zero = Variant.ZeroOf(kind);

        zero.Kind.Should().Be(kind);
        zero.ToString().Should().Be(text);
    }
}
