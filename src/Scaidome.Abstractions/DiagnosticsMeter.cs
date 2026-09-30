using PlatformMeter = System.Diagnostics.Metrics.Meter;

namespace Scaidome.Abstractions;

/// <summary>A meter backed by a platform meter.</summary>
public sealed class DiagnosticsMeter : IMeter
{
    private readonly PlatformMeter _meter;

    public DiagnosticsMeter(PlatformMeter meter)
    {
        _meter = meter;
    }

    public void CreateObservableGauge<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
        where T : struct =>
        _meter.CreateObservableGauge(name, () => ToPlatform(observe()), unit, description);

    public void CreateObservableCounter<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
        where T : struct =>
        _meter.CreateObservableCounter(name, () => ToPlatform(observe()), unit, description);

    public void CreateObservableUpDownCounter<T>(
        string name,
        Func<Measurement<T>> observe,
        string? unit = null,
        string? description = null)
        where T : struct =>
        _meter.CreateObservableUpDownCounter(name, () => ToPlatform(observe()), unit, description);

    public void Dispose() => _meter.Dispose();

    private static System.Diagnostics.Metrics.Measurement<T> ToPlatform<T>(Measurement<T> measurement)
        where T : struct =>
        new(measurement.Value, measurement.Tags.ToArray());
}
