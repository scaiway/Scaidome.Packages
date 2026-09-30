using Scaidome.Abstractions;
using PlatformMeter = System.Diagnostics.Metrics.Meter;

namespace Scaidome.Channels.Tests;

/// <summary>A meter factory that hands out platform-backed meters and keeps what it was asked.</summary>
internal sealed class RecordingMeterFactory : IMeterFactory
{
    private readonly Action<PlatformMeter>? _onCreated;

    public RecordingMeterFactory(Action<PlatformMeter>? onCreated = null)
    {
        _onCreated = onCreated;
    }

    public List<(string Name, string? Version)> Requests { get; } = [];

    public List<CountingMeter> Meters { get; } = [];

    public IMeter CreateMeter(string name, string? version = null)
    {
        Requests.Add((name, version));
        var platform = new PlatformMeter(name, version);
        _onCreated?.Invoke(platform);
        var meter = new CountingMeter(new DiagnosticsMeter(platform));
        Meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in Meters)
        {
            meter.Dispose();
        }
    }

    internal sealed class CountingMeter : IMeter
    {
        private readonly IMeter _inner;

        public CountingMeter(IMeter inner)
        {
            _inner = inner;
        }

        public int Disposals { get; private set; }

        public int Evaluations { get; private set; }

        public void CreateObservableGauge<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
            where T : struct =>
            _inner.CreateObservableGauge(name, Counted(observe), unit, description);

        public void CreateObservableCounter<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
            where T : struct =>
            _inner.CreateObservableCounter(name, Counted(observe), unit, description);

        public void CreateObservableUpDownCounter<T>(
            string name,
            Func<Measurement<T>> observe,
            string? unit = null,
            string? description = null)
            where T : struct =>
            _inner.CreateObservableUpDownCounter(name, Counted(observe), unit, description);

        public void Dispose()
        {
            Disposals++;
            _inner.Dispose();
        }

        private Func<Measurement<T>> Counted<T>(Func<Measurement<T>> observe)
            where T : struct =>
            () =>
            {
                Evaluations++;
                return observe();
            };
    }
}
