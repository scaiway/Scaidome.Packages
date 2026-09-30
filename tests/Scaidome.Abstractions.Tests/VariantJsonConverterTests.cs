using System.Text.Json;

namespace Scaidome.Abstractions.Tests;

public class VariantJsonConverterTests
{
    private sealed record Holder(Variant Value);

    [Theory]
    [InlineData("null", VariantKind.Null)]
    [InlineData("true", VariantKind.Bool)]
    [InlineData("7", VariantKind.Int)]
    [InlineData("7.0", VariantKind.Double)]
    [InlineData("2147483648", VariantKind.Double)]
    [InlineData("1.5e3", VariantKind.Double)]
    [InlineData("\"#fff\"", VariantKind.String)]
    [InlineData("\"2026-09-03T00:00:00Z\"", VariantKind.String)]
    [InlineData("{\"a\": [1, 2]}", VariantKind.Json)]
    [InlineData("[ ]", VariantKind.Json)]
    public void Reading_takes_the_kind_from_the_json(string json, VariantKind kind)
    {
        JsonSerializer.Deserialize<Variant>(json).Kind.Should().Be(kind);
    }

    [Fact]
    public void A_null_property_reads_as_no_value()
    {
        JsonSerializer.Deserialize<Holder>("{\"Value\":null}")!.Value.IsNull.Should().BeTrue();
    }

    [Fact]
    public void Json_is_embedded_without_whitespace()
    {
        var value = JsonSerializer.Deserialize<Variant>("{ \"a\" : [ 1 , 2 ] }");

        value.AsText().Should().Be("{\"a\":[1,2]}");
        JsonSerializer.Serialize(Variant.FromJson("{ \"b\" : true }")).Should().Be("{\"b\":true}");
    }

    [Fact]
    public void Writing_follows_the_json_form()
    {
        JsonSerializer.Serialize(Variant.Null).Should().Be("null");
        JsonSerializer.Serialize(Variant.FromBool(false)).Should().Be("false");
        JsonSerializer.Serialize(Variant.FromInt(3)).Should().Be("3");
        JsonSerializer.Serialize(Variant.FromEpoch(3.25)).Should().Be("3.25");
        JsonSerializer.Serialize(Variant.FromColor("red")).Should().Be("\"red\"");
        JsonSerializer.Serialize(Variant.FromDateTime(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)))
            .Should().Be("\"2026-01-02T03:04:05.0000000Z\"");
    }

    [Fact]
    public void Kinds_are_dropped_on_the_way_back()
    {
        Roundtrip(Variant.FromEpoch(5)).Kind.Should().Be(VariantKind.Int);
        Roundtrip(Variant.FromDouble(7)).Kind.Should().Be(VariantKind.Int);
        Roundtrip(Variant.FromColor("red")).Kind.Should().Be(VariantKind.String);
        Roundtrip(Variant.FromDateTime(DateTime.UtcNow)).Kind.Should().Be(VariantKind.String);
        Roundtrip(Variant.FromDouble(7.5)).Should().Be(Variant.FromDouble(7.5));
    }

    [Theory]
    [InlineData("1e400")]
    [InlineData("-1e400")]
    [InlineData("{")]
    [InlineData("tru")]
    public void Malformed_input_is_reported_as_malformed_json(string json)
    {
        FluentActions.Invoking(() => JsonSerializer.Deserialize<Variant>(json)).Should().Throw<JsonException>();
    }

    private static Variant Roundtrip(Variant value) =>
        JsonSerializer.Deserialize<Variant>(JsonSerializer.Serialize(value));
}
