namespace Scaidome.Json.Tests;

public class SettingsKindRegistryTests
{
    [Fact]
    public void Registrations_chain_and_resolve_in_both_directions()
    {
        var registry = new SettingsKindRegistry();

        registry.Register<PumpSettings>("pump").Register("valve", typeof(ValveSettings)).Should().BeSameAs(registry);

        registry.GetKind("pump").Should().Be<PumpSettings>();
        registry.GetKind("VALVE").Should().Be<ValveSettings>();
        registry.GetStoredName(typeof(ValveSettings)).Should().Be("valve");
        registry.TryGetKind("Pump", out var kind).Should().BeTrue();
        kind.Should().Be<PumpSettings>();
        registry.TryGetStoredName(typeof(PumpSettings), out var name).Should().BeTrue();
        name.Should().Be("pump");
        registry.ListRegistrations().Should().Equal(
            new SettingsKindRegistration("pump", typeof(PumpSettings)),
            new SettingsKindRegistration("valve", typeof(ValveSettings)));
    }

    [Fact]
    public void A_stored_name_is_unique_without_regard_to_case()
    {
        var registry = new SettingsKindRegistry();
        registry.Register<PumpSettings>("pump");

        FluentActions.Invoking(() => registry.Register<ValveSettings>("PUMP"))
            .Should().Throw<InvalidOperationException>().WithMessage("*'PUMP'*already registered*");
        registry.ListRegistrations().Should().HaveCount(1);
    }

    [Fact]
    public void A_kind_is_registered_under_one_name()
    {
        var registry = new SettingsKindRegistry();
        registry.Register<PumpSettings>("pump");

        FluentActions.Invoking(() => registry.Register<PumpSettings>("pump-2")).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void An_unregistered_name_fails_listing_the_registered_names()
    {
        var registry = new SettingsKindRegistry();
        registry.Register<PumpSettings>("pump").Register<ValveSettings>("valve");

        FluentActions.Invoking(() => registry.GetKind("motor"))
            .Should().Throw<JsonDocumentException>()
            .WithMessage("*'motor'*not registered*pump, valve*");
        registry.TryGetKind("motor", out _).Should().BeFalse();
        registry.TryGetKind(null, out _).Should().BeFalse();
        FluentActions.Invoking(() => registry.GetStoredName(typeof(string))).Should().Throw<InvalidOperationException>();
        registry.TryGetStoredName(typeof(string), out _).Should().BeFalse();
    }

    [Fact]
    public void Blank_names_are_refused()
    {
        FluentActions.Invoking(() => new SettingsKindRegistry().Register<PumpSettings>(" ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reading_a_stored_document_resolves_its_name()
    {
        var registry = new SettingsKindRegistry().Register<PumpSettings>("pump");
        var codec = JsonCodecFactory.Create();

        codec.ReadStored(registry, "Pump", "{\"maxSpeed\": 3}").Should().Be(new PumpSettings(maxSpeed: 3));
        codec.ReadStored<SampleSettings>(registry, "pump", "{}").Should().BeOfType<PumpSettings>();
        FluentActions.Invoking(() => codec.ReadStored(registry, "valve", "{}"))
            .Should().Throw<JsonDocumentException>().WithMessage("*Registered names: pump*");
        FluentActions.Invoking(() => codec.ReadStored<ValveSettings>(registry, "pump", "{}"))
            .Should().Throw<JsonDocumentException>();
        FluentActions.Invoking(() => codec.ReadStored(registry, "pump", null)).Should().Throw<JsonDocumentException>();
    }

    [Fact]
    public async Task Reads_are_safe_while_registering()
    {
        var registry = new SettingsKindRegistry();
        var kinds = new List<(string Name, Type Kind)>();
        var kind = typeof(int);
        for (var i = 0; i < 200; i++)
        {
            kind = kind.MakeArrayType();
            kinds.Add(($"kind-{i}", kind));
        }

        var writer = Task.Run(() =>
        {
            foreach (var (name, type) in kinds)
            {
                registry.Register(name, type);
            }
        });
        var reader = Task.Run(() =>
        {
            while (!writer.IsCompleted)
            {
                _ = registry.ListRegistrations().Count;
                _ = registry.TryGetKind("kind-0", out _);
            }
        });
        await Task.WhenAll(writer, reader);

        registry.ListRegistrations().Should().HaveCount(200);
    }
}
