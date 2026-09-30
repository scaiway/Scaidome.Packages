namespace Scaidome.Abstractions;

/// <summary>
/// Creates meters. The platform's own metrics types are kept out of the libraries' surface so a host decides how metrics are
/// collected and exported. Disposing the factory disposes every meter it created.
/// </summary>
public interface IMeterFactory : IDisposable
{
    /// <summary>Creates a meter with a name and an optional version.</summary>
    IMeter CreateMeter(string name, string? version = null);
}
