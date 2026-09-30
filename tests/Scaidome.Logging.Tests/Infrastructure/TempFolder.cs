namespace Scaidome.Logging.Tests.Infrastructure;

/// <summary>A folder of its own for one test, deleted afterwards.</summary>
public sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "scai-logging-tests", Guid.NewGuid().ToString("N"));
    }

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A file still open by a worker that outlived its test; the temp folder is cleaned by the system.
        }
    }
}
