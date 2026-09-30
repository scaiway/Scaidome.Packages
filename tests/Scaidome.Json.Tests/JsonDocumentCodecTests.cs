using System.Text.Json;

namespace Scaidome.Json.Tests;

public class JsonDocumentCodecTests
{
    private readonly JsonDocumentCodec _codec = JsonCodecFactory.Create();

    [Fact]
    public void Writes_camel_case_fields_from_the_runtime_kind_with_no_kind_field()
    {
        SampleSettings settings = new PumpSettings("P1", 900, SampleMode.On, true);

        var json = _codec.Write(settings);

        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(field => (field.Name, field.Value.ToString()))
            .Should().BeEquivalentTo(new[] { ("label", "P1"), ("maxSpeed", "900"), ("mode", "1"), ("isReversible", "True") });
    }

    [Fact]
    public void Reads_fields_without_regard_to_case_and_defaults_what_is_missing()
    {
        var settings = _codec.Read<PumpSettings>("{\"MAXSPEED\": 10, \"unknownField\": [1,2]}");

        settings.Should().Be(new PumpSettings(null, 10, SampleMode.Auto, false));
    }

    [Fact]
    public void Round_trips_through_the_general_kind()
    {
        var original = new ValveSettings("V", 4.25);

        var read = _codec.Read(_codec.Write(original), typeof(ValveSettings));

        read.Should().Be(original);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"maxSpeed\": \"fast\"}")]
    [InlineData("{\"mode\": \"On\"}")]
    [InlineData("{\"maxSpeed\": 1,}")]
    public void Reading_fails_with_the_conversion_error(string? document)
    {
        var failure = FluentActions.Invoking(() => _codec.Read<PumpSettings>(document))
            .Should().Throw<JsonDocumentException>().Which;

        failure.Should().BeAssignableTo<JsonException>();
        failure.KindName.Should().Be(nameof(PumpSettings));
        failure.Document.Should().Be(document);
        failure.Message.Should().Contain(nameof(PumpSettings));
    }

    [Fact]
    public void A_constructor_refusal_is_a_conversion_error()
    {
        FluentActions.Invoking(() => _codec.Read<ValveSettings>("{\"openTime\": -1}"))
            .Should().Throw<JsonDocumentException>();
    }

    [Fact]
    public void The_document_in_the_error_is_cut_to_500_characters()
    {
        var document = "{\"label\": \"" + new string('x', 2000) + "\", \"maxSpeed\": \"bad\"}";

        var failure = FluentActions.Invoking(() => _codec.Read<PumpSettings>(document))
            .Should().Throw<JsonDocumentException>().Which;

        failure.Document.Should().HaveLength(500);
        document.Should().StartWith(failure.Document);
    }

    [Fact]
    public void The_codec_options_are_read_only()
    {
        _codec.Options.IsReadOnly.Should().BeTrue();
        _codec.Options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        _codec.Options.PropertyNameCaseInsensitive.Should().BeTrue();
        _codec.Options.AllowTrailingCommas.Should().BeFalse();
        _codec.Options.Converters.Should().BeEmpty("enumerated settings stay numbers");
    }

    [Fact]
    public void The_shared_codec_is_one_instance()
    {
        JsonCodecFactory.Shared.Should().BeSameAs(JsonCodecFactory.Shared);
    }
}
