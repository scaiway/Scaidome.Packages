using Scaidome.Abstractions;

namespace Scaidome.Channels;

/// <summary>A meter that registers nothing and never evaluates a measurement.</summary>
internal sealed class NullMeter : IMeter
{
    public static readonly NullMeter Instance = new();

    private NullMeter()
    {
    }

    public void CreateObservableGauge<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
        where T : struct
    {
    }

    public void CreateObservableCounter<T>(string name, Func<Measurement<T>> observe, string? unit = null, string? description = null)
        where T : struct
    {
    }

    public void CreateObservableUpDownCounter<T>(
        string name,
        Func<Measurement<T>> observe,
        string? unit = null,
        string? description = null)
        where T : struct
    {
    }

    public void Dispose()
    {
    }
}
