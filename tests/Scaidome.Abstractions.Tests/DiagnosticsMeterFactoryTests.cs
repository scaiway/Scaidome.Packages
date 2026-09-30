using System.Diagnostics.Metrics;
using Scaidome.Abstractions;

namespace Scaidome.Abstractions.Tests;

public class DiagnosticsMeterFactoryTests
{
    [Fact]
    public void Instruments_are_observed_with_their_tags()
    {
        using var factory = new DiagnosticsMeterFactory();
        var meterName = "test." + Guid.NewGuid();
        var meter = factory.CreateMeter(meterName, "2.0");
        var tag = new KeyValuePair<string, object?>("channel", "q");
        meter.CreateObservableGauge("gauge", () => new Scaidome.Abstractions.Measurement<long>(3, tag));
        meter.CreateObservableCounter("counter", () => new Scaidome.Abstractions.Measurement<int>(4, tag));
        meter.CreateObservableUpDownCounter("updown", () => new Scaidome.Abstractions.Measurement<double>(-1.5, tag));

        var seen = new List<(string Name, object Value, string? Version, object? Tag)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((i, v, t, _) => seen.Add((i.Name, v, i.Meter.Version, t[0].Value)));
        listener.SetMeasurementEventCallback<int>((i, v, t, _) => seen.Add((i.Name, v, i.Meter.Version, t[0].Value)));
        listener.SetMeasurementEventCallback<double>((i, v, t, _) => seen.Add((i.Name, v, i.Meter.Version, t[0].Value)));
        listener.Start();
        listener.RecordObservableInstruments();

        seen.Should().BeEquivalentTo(new (string, object, string?, object?)[]
        {
            ("gauge", 3L, "2.0", "q"),
            ("counter", 4, "2.0", "q"),
            ("updown", -1.5, "2.0", "q"),
        });
    }

    [Fact]
    public void Disposing_the_factory_disposes_its_meters()
    {
        var factory = new DiagnosticsMeterFactory();
        var meterName = "test." + Guid.NewGuid();
        var meter = factory.CreateMeter(meterName);
        var observed = 0;
        meter.CreateObservableGauge("gauge", () =>
        {
            observed++;
            return new Scaidome.Abstractions.Measurement<long>(1);
        });

        factory.Dispose();
        factory.Dispose();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.Start();
        listener.RecordObservableInstruments();
        observed.Should().Be(0);
        FluentActions.Invoking(() => factory.CreateMeter("late")).Should().Throw<ObjectDisposedException>();
    }
}
