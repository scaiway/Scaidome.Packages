using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Scaidome.Channels;

/// <summary>
/// Passes everything to the channel's reader and counts each item taken. Only the single take is overridden: the platform's
/// reader builds every other way of reading (reading asynchronously, enumerating) on it, so each item is counted exactly once.
/// </summary>
internal sealed class CountingChannelReader<T> : ChannelReader<T>
{
    private readonly ChannelReader<T> _inner;
    private readonly QueueCounters _counters;

    public CountingChannelReader(ChannelReader<T> inner, QueueCounters counters)
    {
        _inner = inner;
        _counters = counters;
    }

    public override Task Completion => _inner.Completion;

    public override bool CanCount => _inner.CanCount;

    public override int Count => _inner.Count;

    /// <summary>Peeking is not offered, although the channel could support it.</summary>
    public override bool CanPeek => false;

    public override bool TryPeek([MaybeNullWhen(false)] out T item)
    {
        item = default;
        return false;
    }

    public override bool TryRead([MaybeNullWhen(false)] out T item)
    {
        if (!_inner.TryRead(out item))
        {
            return false;
        }

        _counters.CountRead();
        return true;
    }

    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default) =>
        _inner.WaitToReadAsync(cancellationToken);
}
