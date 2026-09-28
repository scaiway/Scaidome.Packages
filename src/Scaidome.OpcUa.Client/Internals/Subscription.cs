using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using OpcSubscription = Opc.Ua.Client.Subscription;

namespace Scaidome.OpcUa.Client;

internal class Subscription : ISubscription
{
    private readonly Guid _id = Guid.CreateVersion7();
    private readonly string _displayName;
    private readonly int _samplingInterval;
    private readonly uint _queueSize;
    private readonly ILogger _logger;
    private readonly Dictionary<Guid, MonitoredItem> _monitoredItems = [];

    public Subscription(string displayName, int samplingInterval, uint queueSize, OpcSubscription internalSubscription, ILogger logger)
    {
        _displayName = displayName;
        _samplingInterval = samplingInterval;
        _queueSize = queueSize;
        InternalSubscription = internalSubscription;
        _logger = logger;
    }

    public Guid Id => _id;
    public string DisplayName => _displayName;
    internal OpcSubscription? InternalSubscription { get; set; }

    public async Task<MonitoredNode?> AddMonitoredItemAsync(MonitoredNodeDescriptor node, CancellationToken ct = default)
    {
        if (InternalSubscription == null)
            throw new InvalidOperationException("Subscription is not active.");

        var result = AddItem(node);

        await InternalSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);

        return result;
    }

    public async Task<MonitoredNode?[]> AddMonitoredItemsAsync(MonitoredNodeDescriptor[] nodes, CancellationToken ct = default)
    {
        if (InternalSubscription == null)
            throw new InvalidOperationException("Subscription is not active.");

        var results = new MonitoredNode?[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
            results[i] = AddItem(nodes[i]);

        await InternalSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);

        return results;
    }

    // A single malformed NodeId (e.g. wrong namespace index) must not abort the whole batch
    private MonitoredNode? AddItem(MonitoredNodeDescriptor descriptor)
    {
        NodeId nodeId;
        try
        {
            nodeId = new NodeId(descriptor.NodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add monitored item for {DisplayName} ({NodeId}): invalid node id.", descriptor.DisplayName, descriptor.NodeId);
            return null;
        }

        var monitoredItem = new MonitoredItem(InternalSubscription!.DefaultItem)
        {
            StartNodeId = nodeId,
            AttributeId = Attributes.Value,
            DisplayName = descriptor.DisplayName,
            SamplingInterval = _samplingInterval,
            QueueSize = _queueSize,
            DiscardOldest = true,
        };

        var node = new MonitoredNode(descriptor.Id, descriptor.NodeId, descriptor.DisplayName, descriptor.Context) { MonitoredItem = monitoredItem };
        monitoredItem.Handle = node;

        InternalSubscription.AddItem(monitoredItem);
        _monitoredItems[descriptor.Id] = monitoredItem;

        return node;
    }

    public async Task RemoveMonitoredItemAsync(Guid id, CancellationToken ct = default)
    {
        if (InternalSubscription == null)
            throw new InvalidOperationException("Subscription is not active.");

        if (_monitoredItems.TryGetValue(id, out var monitoredItem))
        {
            InternalSubscription.RemoveItem(monitoredItem);
            await InternalSubscription.ApplyChangesAsync(ct).ConfigureAwait(false);
            _monitoredItems.Remove(id);
        }
    }

    public MonitoredNode? FindMonitoredNode(Guid id)
    {
        return _monitoredItems.TryGetValue(id, out var item) ? item.Handle as MonitoredNode : null;
    }

    public IReadOnlyCollection<MonitoredNode> GetMonitoredNodes()
    {
        return _monitoredItems.Values
            .Select(item => item.Handle as MonitoredNode)
            .Where(node => node != null) // guarantees no nulls remain -> ! in "ToArray()!" is safe
            .ToArray()!;
    }
}
