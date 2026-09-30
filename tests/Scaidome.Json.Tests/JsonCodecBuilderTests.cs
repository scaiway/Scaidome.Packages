using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scaidome.Json.Tests;

public class JsonCodecBuilderTests
{
    [Fact]
    public void Defaults_are_the_serializer_defaults()
    {
        var codec = new JsonCodecBuilder().Build();

        codec.Write(new PumpSettings("a")).Should().Contain("\"MaxSpeed\"");
        codec.Read<PumpSettings>("{\"maxspeed\": 5}").MaxSpeed.Should().Be(1500, "fields match by case unless switched");
        FluentActions.Invoking(() => codec.Read<PumpSettings>("{\"MaxSpeed\": 5,}")).Should().Throw<JsonDocumentException>();
        FluentActions.Invoking(() => codec.Read<PumpSettings>("{/* c */ \"MaxSpeed\": 5}")).Should().Throw<JsonDocumentException>();
    }

    [Fact]
    public void Switches_reach_the_options()
    {
        var codec = new JsonCodecBuilder()
            .WithCaseInsensitiveFields()
            .WithTrailingCommas()
            .WithComments()
            .WithNamingPolicy(JsonNamingPolicy.SnakeCaseLower)
            .AddConverter(new JsonStringEnumConverter())
            .Build();

        codec.Write(new PumpSettings("a", mode: SampleMode.On)).Should().Contain("\"max_speed\"").And.Contain("\"On\"");
        codec.Read<PumpSettings>("{/* note */ \"MAX_SPEED\": 7, \"mode\": \"Off\",}")
            .Should().Be(new PumpSettings(null, 7, SampleMode.Off));
    }

    [Fact]
    public void The_factory_builder_uses_camel_case_and_case_insensitive_fields()
    {
        var options = JsonCodecFactory.CreateBuilder().BuildOptions();

        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        options.PropertyNameCaseInsensitive.Should().BeTrue();
        options.ReadCommentHandling.Should().Be(JsonCommentHandling.Disallow);
    }
}
