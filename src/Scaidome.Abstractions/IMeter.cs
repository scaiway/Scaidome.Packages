namespace Scaidome.Abstractions;

/// <summary>
/// A named meter that registers observable instruments. An instrument's callback is evaluated only when a collector asks.
/// Disposing the meter removes its instruments from whatever collects them; a meter tolerates repeated disposal.
/// </summary>
public interface IMeter : IDisposable
{
    void CreateObservableGauge<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
        where T : struct;

    void CreateObservableCounter<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
        where T : struct;

    void CreateObservableUpDownCounter<T>(
        string name,
        Func<Measurement<T>> observe,
        string? unit = null,
        string? description = null)
        where T : struct;
}
