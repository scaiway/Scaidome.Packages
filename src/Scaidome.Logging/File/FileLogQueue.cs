using System.Collections.Concurrent;
using System.Text;

namespace Scaidome.Logging.File;

/// <summary>
/// The queue every logger writes through, and the one background thread that writes queued entries to the file in order, so a
/// caller never waits on the file. The file is opened for each entry and closed again, so nothing holds it between entries.
/// </summary>
internal sealed class FileLogQueue : IDisposable
{
    // The last lines before a shutdown often explain it, so disposal waits for the queue to drain, but not for ever.
    internal static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan CancelTimeout = TimeSpan.FromSeconds(1);

    private readonly BlockingCollection<FileLogEntry> _entries = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string _path;
    private readonly long _sizeLimit;
    private readonly int _filesKept;
    private readonly Task _worker;
    private int _disposed;

    public FileLogQueue(string path, long sizeLimit, int filesKept)
    {
        _path = System.IO.Path.GetFullPath(path);
        _sizeLimit = sizeLimit;
        _filesKept = Math.Max(1, filesKept);
        _worker = Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public string Path => _path;

    /// <summary>Queues an entry. An entry queued after disposal is dropped, and the call does not fail.</summary>
    public void Enqueue(FileLogEntry entry)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            _entries.TryAdd(entry);
        }
        catch (InvalidOperationException)
        {
            // Adding was completed, or the queue disposed, between the check and the add.
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _entries.CompleteAdding();
        if (!_worker.Wait(DrainTimeout))
        {
            // The worker had its time to write what was queued; entries not written after the cancel are dropped.
            _cancellation.Cancel();
            if (!_worker.Wait(CancelTimeout))
            {
                // A worker still running keeps the queue: disposing it under the worker would fault the worker.
                return;
            }
        }

        _entries.Dispose();
        _cancellation.Dispose();
    }

    private void Run()
    {
        try
        {
            foreach (var entry in _entries.GetConsumingEnumerable(_cancellation.Token))
            {
                Write(entry);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Write(FileLogEntry entry)
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            RollIfFull();
            using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            var bytes = Encoding.UTF8.GetBytes(entry.Format());
            stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception)
        {
            // A failure to write is dropped; logging never fails its caller, and there is nowhere else to report it.
        }
    }

    /// <summary>
    /// Before an entry is written, a live file at the size limit rolls: the oldest numbered file is deleted, every other
    /// numbered file moves up one, and the live file becomes number 1.
    /// </summary>
    private void RollIfFull()
    {
        var live = new FileInfo(_path);
        if (!live.Exists || live.Length < _sizeLimit)
        {
            return;
        }

        if (_filesKept <= 1)
        {
            System.IO.File.Delete(_path);
            return;
        }

        var oldest = NumberedPath(_filesKept - 1);
        if (System.IO.File.Exists(oldest))
        {
            System.IO.File.Delete(oldest);
        }

        for (var number = _filesKept - 2; number >= 1; number--)
        {
            var source = NumberedPath(number);
            if (System.IO.File.Exists(source))
            {
                System.IO.File.Move(source, NumberedPath(number + 1), overwrite: true);
            }
        }

        System.IO.File.Move(_path, NumberedPath(1), overwrite: true);
    }

    private string NumberedPath(int number) => $"{_path}.{number}";
}
