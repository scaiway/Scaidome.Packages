# Scaidome.Channels

Action queues: unbounded in-memory queues that any number of producers add to and a single loop drains. A queue counts
the items added and taken and publishes those counts as metrics. It knows nothing about what the items are.

Depends on `Scaidome.Abstractions` for the metrics abstraction. Namespace: `Scaidome.Channels`.

## Usage

```csharp
using var queue = new ActionQueue<MyMessage>(
    "security-engine",
    new UnboundedChannelOptions { SingleReader = true },
    meterFactory); // optional

// Producers
queue.Writer.TryWrite(message);

// The one loop that drains it
await foreach (var item in queue.Reader.ReadAllAsync(cancellationToken))
{
    Handle(item);
}

// The owner ends the queue through its writer
queue.Writer.Complete();
```

## Behaviour

- **The options pass through unchanged.** `SingleReader` and `SingleWriter` are hints to the channel. The queue neither
  enforces nor changes them, because its counters are safe whatever the hints say.
- **Each item is counted once.** The reader counts in its single-item take, and the platform builds every other way of
  reading (async reads, enumeration) on that take. The writer counts only items the channel accepts, so a refused or
  cancelled write counts nothing.
- **Peeking is not offered**, although the underlying channel could support it.
- **The counts are lock-free.** Each one is a single atomic increment. Written and read are read separately, never as
  one snapshot, so the depth can briefly show a negative number.
- **Without a meter factory**, the instruments go to a meter that does nothing, and the measurements are never
  evaluated. If the factory fails to create its meter, creating the queue fails with the factory's own error.
- **Disposing only unpublishes the metrics.** It doesn't complete the channel: queued items stay queued, and the reader,
  writer and counts keep working. The owner completes the queue through its writer.

## Metrics

All instruments are on the meter `Scaidome.Channels` (version `1.0`) and carry a `channel` tag with the queue's name.

| Instrument | Kind | Meaning |
|---|---|---|
| `scai.channel.depth` | Observable gauge | Items written but not yet read |
| `scai.channel.written` | Observable counter | Items accepted in total |
| `scai.channel.read` | Observable counter | Items taken in total |

The names are public constants on `ActionQueue<T>`.
