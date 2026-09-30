using Microsoft.Extensions.DependencyInjection;

namespace Scaidome.Json.Tests;

public class JsonServiceCollectionExtensionsTests
{
    [Fact]
    public void The_codec_is_one_instance_under_its_two_halves_and_itself()
    {
        using var provider = new ServiceCollection().AddScaiJson().BuildServiceProvider();

        var codec = provider.GetRequiredService<JsonDocumentCodec>();
        provider.GetRequiredService<IJsonDocumentReader>().Should().BeSameAs(codec);
        provider.GetRequiredService<IJsonDocumentWriter>().Should().BeSameAs(codec);
    }

    [Fact]
    public void The_registry_is_one_instance()
    {
        using var provider = new ServiceCollection().AddScaiJson().AddScaiJson().BuildServiceProvider();

        provider.GetRequiredService<ISettingsKindRegistry>()
            .Should().BeOfType<SettingsKindRegistry>()
            .And.BeSameAs(provider.GetRequiredService<ISettingsKindRegistry>());
        provider.GetServices<ISettingsKindRegistry>().Should().HaveCount(1);
    }

    [Fact]
    public void The_codec_applies_the_factory_rules()
    {
        using var provider = new ServiceCollection().AddScaiJson().BuildServiceProvider();

        provider.GetRequiredService<IJsonDocumentWriter>().Write(new ValveSettings("x", 1)).Should().Contain("\"openTime\"");
    }
}
