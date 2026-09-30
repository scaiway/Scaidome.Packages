namespace Scaidome.Channels;

/// <summary>
/// The two totals a queue keeps, shared by its reader and writer. Each count is one atomic increment, with no lock and no
/// allocation, so it is safe under any number of producers and readers. The totals only increase and are read independently:
/// nothing reads both as one snapshot, which is why depth can briefly be negative.
/// </summary>
internal sealed class QueueCounters
{
    private long _written;
    private long _read;

    public long Written => Interlocked.Read(ref _written);

    public long Read => Interlocked.Read(ref _read);

    public void CountWritten() => Interlocked.Increment(ref _written);

    public void CountRead() => Interlocked.Increment(ref _read);
}
