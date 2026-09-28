using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Opc.Ua;

namespace Scaidome.OpcUa.Client;

/// <summary>
/// <see cref="ITelemetryContext"/> implementation that delegates logging to an existing
/// <see cref="ILoggerFactory"/>, allowing OPC UA client logs to flow through the host's
/// logging pipeline and be controlled via appsettings.json.
/// </summary>
internal sealed class HostTelemetry : ITelemetryContext, IDisposable
{
    public HostTelemetry(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        ActivitySource = new ActivitySource("Scaidome.OpcUa.Client", "1.0.0");
    }

    /// <inheritdoc/>
    public ILoggerFactory LoggerFactory { get; }

    /// <inheritdoc/>
    public ActivitySource ActivitySource { get; }

    /// <inheritdoc/>
    public Meter CreateMeter() => new("Scaidome.OpcUa.Client", "1.0.0");

    /// <inheritdoc/>
    public void Dispose() => ActivitySource.Dispose();
}
