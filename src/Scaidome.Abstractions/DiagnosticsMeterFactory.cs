using PlatformMeterFactory = System.Diagnostics.Metrics.IMeterFactory;

namespace Scaidome.Abstractions;

/// <summary>
/// The metrics abstraction over the platform's System.Diagnostics.Metrics, so a host's collectors and exporters see the
/// instruments. With a platform factory, meters are created through it and share its lifetime rules.
/// </summary>
public sealed class DiagnosticsMeterFactory : IMeterFactory
{
    private readonly PlatformMeterFactory? _platform;
    private readonly List<DiagnosticsMeter> _meters = [];
    private readonly Lock _lock = new();
    private bool _disposed;

    public DiagnosticsMeterFactory()
        : this(null)
    {
    }

    public DiagnosticsMeterFactory(PlatformMeterFactory? platform)
    {
        _platform = platform;
    }

    public IMeter CreateMeter(string name, string? version = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var meter = _platform is null
                ? new System.Diagnostics.Metrics.Meter(name, version)
                : _platform.Create(new System.Diagnostics.Metrics.MeterOptions(name) { Version = version });
            var wrapper = new DiagnosticsMeter(meter);
            _meters.Add(wrapper);
            return wrapper;
        }
    }

    public void Dispose()
    {
        DiagnosticsMeter[] meters;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            meters = [.. _meters];
            _meters.Clear();
        }

        foreach (var meter in meters)
        {
            meter.Dispose();
        }
    }
}
