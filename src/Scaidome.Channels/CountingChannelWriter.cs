using System.Threading.Channels;

namespace Scaidome.Channels;

/// <summary>
/// Passes everything to the channel's writer and counts each item the channel accepts. The platform's writer builds the
/// asynchronous write on the try-write, so a refused or cancelled write counts nothing.
/// </summary>
internal sealed class CountingChannelWriter<T> : ChannelWriter<T>
{
    private readonly ChannelWriter<T> _inner;
    private readonly QueueCounters _counters;

    public CountingChannelWriter(ChannelWriter<T> inner, QueueCounters counters)
    {
        _inner = inner;
        _counters = counters;
    }

    public override bool TryComplete(Exception? error = null) => _inner.TryComplete(error);

    /// <summary>An unbounded channel never waits for capacity, so this fails only once the channel is completed.</summary>
    public override bool TryWrite(T item)
    {
        if (!_inner.TryWrite(item))
        {
            return false;
        }

        _counters.CountWritten();
        return true;
    }

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default) =>
        _inner.WaitToWriteAsync(cancellationToken);
}
