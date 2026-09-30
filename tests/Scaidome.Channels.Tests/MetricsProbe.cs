using System.Diagnostics.Metrics;

namespace Scaidome.Channels.Tests;

/// <summary>Collects the queue metrics of meters created by one factory, on demand.</summary>
internal sealed class MetricsProbe : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly Dictionary<(string Channel, string Metric), long> _latest = [];
    private readonly HashSet<Meter> _meters = [];

    public MetricsProbe()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ActionQueue<int>.MeterName && _meters.Contains(instrument.Meter))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var channel = tags.ToArray().Single(tag => tag.Key == ActionQueue<int>.ChannelTag).Value as string ?? string.Empty;
            _latest[(channel, instrument.Name)] = value;
            Tags.Add(tags.ToArray());
        });
    }

    public List<KeyValuePair<string, object?>[]> Tags { get; } = [];

    public void Track(Meter meter)
    {
        _meters.Add(meter);
    }

    public void Start() => _listener.Start();

    public IReadOnlyDictionary<(string Channel, string Metric), long> Collect()
    {
        _latest.Clear();
        _listener.RecordObservableInstruments();
        return new Dictionary<(string, string), long>(_latest);
    }

    public void Dispose() => _listener.Dispose();
}
