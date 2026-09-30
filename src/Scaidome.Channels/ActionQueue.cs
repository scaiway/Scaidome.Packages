using System.Threading.Channels;
using Scaidome.Abstractions;

namespace Scaidome.Channels;

/// <summary>
/// An unbounded in-memory queue that any number of producers add to and a single loop drains. It counts every item added and
/// taken and publishes the counts as metrics; it knows nothing of what the items are. The owner completes the queue through its
/// writer; disposing only unpublishes the metrics.
/// </summary>
public sealed class ActionQueue<T> : IDisposable
{
    public const string MeterName = "Scaidome.Channels";
    public const string MeterVersion = "1.0";
    public const string DepthMetric = "scai.channel.depth";
    public const string WrittenMetric = "scai.channel.written";
    public const string ReadMetric = "scai.channel.read";
    public const string ChannelTag = "channel";

    private readonly IMeter _meter;

    public ActionQueue(string name, UnboundedChannelOptions options, IMeterFactory? meterFactory = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        // The options reach the channel unchanged; single-reader and single-writer are hints the queue neither enforces nor
        // alters, because the counters below are safe whatever the hints say.
        var channel = Channel.CreateUnbounded<T>(options);
        var counters = new QueueCounters();
        Reader = new CountingChannelReader<T>(channel.Reader, counters);
        Writer = new CountingChannelWriter<T>(channel.Writer, counters);

        // Without a factory the instruments go to a meter that does nothing, so the measurements are never evaluated. A factory
        // that fails to create the meter fails the queue with its own error.
        _meter = meterFactory?.CreateMeter(MeterName, MeterVersion) ?? NullMeter.Instance;

        var tag = new KeyValuePair<string, object?>(ChannelTag, name);
        _meter.CreateObservableGauge(DepthMetric, () => new Measurement<long>(counters.Written - counters.Read, tag));
        _meter.CreateObservableCounter(WrittenMetric, () => new Measurement<long>(counters.Written, tag));
        _meter.CreateObservableCounter(ReadMetric, () => new Measurement<long>(counters.Read, tag));
    }

    public ChannelReader<T> Reader { get; }

    public ChannelWriter<T> Writer { get; }

    /// <summary>
    /// Disposes the meter, which removes the instruments from collectors. The channel is not completed, queued items stay
    /// queued, and the reader, writer and counts keep working. Each repeated disposal is passed to the meter.
    /// </summary>
    public void Dispose() => _meter.Dispose();
}
