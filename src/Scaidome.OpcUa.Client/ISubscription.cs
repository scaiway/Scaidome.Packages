namespace Scaidome.OpcUa.Client;

public interface ISubscription
{
    Guid Id { get; }
    string DisplayName { get; }

    // MonitoredNode?[] does not mean can return null, but that items in the array can be null: a node id that will not parse,
    // or a node the server refused to monitor, such as one it does not have.
    Task<MonitoredNode?> AddMonitoredItemAsync(MonitoredNodeDescriptor node, CancellationToken ct = default);
    Task<MonitoredNode?[]> AddMonitoredItemsAsync(MonitoredNodeDescriptor[] nodes, CancellationToken ct = default);

    Task RemoveMonitoredItemAsync(Guid id, CancellationToken ct = default);
    MonitoredNode? FindMonitoredNode(Guid id);
    IReadOnlyCollection<MonitoredNode> GetMonitoredNodes();
}
