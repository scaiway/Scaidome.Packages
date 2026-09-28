using Opc.Ua.Client;

namespace Scaidome.OpcUa.Client;

public record MonitoredNode(Guid Id, string NodeId, string DisplayName, object Context)
{
    internal MonitoredItem MonitoredItem { get; init; } = null!;
}
