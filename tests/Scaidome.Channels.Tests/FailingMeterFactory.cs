using Scaidome.Abstractions;

namespace Scaidome.Channels.Tests;

/// <summary>A meter factory that cannot create a meter.</summary>
internal sealed class FailingMeterFactory : IMeterFactory
{
    public IMeter CreateMeter(string name, string? version = null) => throw new InvalidOperationException("no meters today");

    public void Dispose()
    {
    }
}
