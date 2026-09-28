using System.Threading.Channels;

namespace Scaidome.OpcUa.Client;

public interface IClient : IDisposable
{
    string Url { get; }

    /// <summary>Where the session is: connected, reconnecting after a bad keepalive, or not connected at all.</summary>
    ClientState State { get; }
    Action<DataValue[]>? OnDataChanged { get; set; }
    ChannelWriter<DataValueChangedMessage>? OnDataChangedChannel { get; set; }

    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(bool leaveChannelOpen = false);

    Task<ISubscription> AddSubscriptionAsync(SubscriptionOptions options, CancellationToken ct = default);
    Task RemoveSubscriptionAsync(ISubscription subscription, CancellationToken ct = default);

    Task<BrowseNode[]> BrowseAsync(string nodeId, bool includeDataType = true, CancellationToken ct = default);

    Task<ReadResult> ReadValueAsync(string nodeId, CancellationToken ct = default);
    Task<NodeAttributes> ReadNodeAttributesAsync(string nodeId, CancellationToken ct = default);
    Task<NodeAttributes[]> ReadNodeAttributesBatchAsync(string[] nodeIds, CancellationToken ct = default);

    Task<WriteResult> WriteValueAsync(WriteValue value, CancellationToken ct = default);
    Task<WriteResult[]> WriteValuesAsync(WriteValue[] values, CancellationToken ct = default);
}