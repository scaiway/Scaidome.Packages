using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using System.Collections.Concurrent;
using System.Threading.Channels;
using OpcSubscription = Opc.Ua.Client.Subscription;

namespace Scaidome.OpcUa.Client;

internal partial class Client
{
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();
    private Action<DataValue[]>? _onDataChanged;
    private ChannelWriter<DataValueChangedMessage>? _onDataChangedChannel;

    /// <summary>
    /// Callback invoked when monitored items report data changes.
    /// </summary>
    public Action<DataValue[]>? OnDataChanged
    {
        get => _onDataChanged;
        set => _onDataChanged = value;
    }

    public ChannelWriter<DataValueChangedMessage>? OnDataChangedChannel
    {
        get => _onDataChangedChannel;
        set => _onDataChangedChannel = value;
    }

    public async Task<ISubscription> AddSubscriptionAsync(SubscriptionOptions options, CancellationToken ct = default)
    {
        if (_session == null)
            throw new InvalidOperationException("Session is not connected.");

        var perSubCallback = options.OnDataChanged;
        var perSubChannel = options.OnDataChangedChannel;
        var opcSubscription = new OpcSubscription(_session.DefaultSubscription)
        {
            DisplayName = options.DisplayName,
            PublishingEnabled = options.PublishingEnabled,
            PublishingInterval = options.PublishingInterval,
            LifetimeCount = 0,
            MinLifetimeInterval = options.MinLifetimeInterval,
            KeepAliveCount = options.KeepAliveCount,
            SequentialPublishing = options.SequentialPublishing,
            DisableMonitoredItemCache = options.DisableMonitoredItemCache,
            FastDataChangeCallback = (perSubCallback != null || perSubChannel != null) ? (sub, notif, _) => InvokeDataChange(sub, notif, perSubCallback, perSubChannel) : FastDataChangeNotification,
            FastKeepAliveCallback = FastKeepAliveNotification
        };

        _session.AddSubscription(opcSubscription);
        await opcSubscription.CreateAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Subscription created: Id={Id}, PublishingInterval={PublishingInterval}.", opcSubscription.Id, options.PublishingInterval);

        var subscription = new Subscription(options.DisplayName, options.SamplingInterval, options.QueueSize, opcSubscription, _logger);
        _subscriptions.TryAdd(subscription.Id, subscription);
        return subscription;
    }

    public async Task RemoveSubscriptionAsync(ISubscription subscription, CancellationToken ct = default)
    {
        if (_subscriptions.TryRemove(subscription.Id, out var sub))
        {
            // TODO: Test and verify against reference impl
            await DetachAsync(sub, ct).ConfigureAwait(false);
        }
    }

    private async Task RemoveAllSubscriptions(CancellationToken ct = default)
    {
        foreach (var subscription in _subscriptions.Values)
        {
            await DetachAsync(subscription, ct).ConfigureAwait(false);
        }
        // TODO: not atomic...
        _subscriptions.Clear();
    }

    internal void FastDataChangeNotification(
        OpcSubscription subscription,
        DataChangeNotification notification,
        IList<string> stringTable)
    {
        try
        {
            var values = BuildDataValues(subscription, notification);
            _onDataChanged?.Invoke(values);
            _onDataChangedChannel?.TryWrite(new DataValueChangedMessage(values));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification error.");
        }
    }

    private void InvokeDataChange(OpcSubscription subscription, DataChangeNotification notification, Action<DataValue[]>? callback, ChannelWriter<DataValueChangedMessage>? channel)
    {
        try
        {
            var values = BuildDataValues(subscription, notification);
            callback?.Invoke(values);
            channel?.TryWrite(new DataValueChangedMessage(values));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification error.");
        }
    }

    // Unresolved client handles (e.g. a notification racing an item removal) are skipped, not emitted as null holes —
    // consumers iterate the array and a null element would abort the whole batch.
    private DataValue[] BuildDataValues(OpcSubscription subscription, DataChangeNotification notification)
    {
        var values = new List<DataValue>(notification.MonitoredItems.Count);
        for (int i = 0; i < notification.MonitoredItems.Count; i++)
        {
            MonitoredItemNotification item = notification.MonitoredItems[i];
            MonitoredItem? monitoredItem = subscription.FindItemByClientHandle(item.ClientHandle);
            if (monitoredItem?.Handle is not MonitoredNode node)
            {
                _logger.LogWarning("FastDataChange: Received notification from unknown subscription or invalid handle (ClientHandle={ClientHandle})", item.ClientHandle);
                continue;
            }

            values.Add(new DataValue(
                node.NodeId,
                item.Value.Value,
                item.Value.StatusCode.Code,
                item.Value.SourceTimestamp,
                item.Value.ServerTimestamp,
                node));
        }
        return [.. values];
    }

    internal void FastKeepAliveNotification(OpcSubscription subscription, NotificationData notification)
    {
        // TODO

        //try
        //{
        //    _logger.LogInformation("KeepAlive: Id={SubscriptionId} PublishTime={PublishTime} SequenceNumber={SequenceNumber}.", subscription.Id, notification.PublishTime, notification.SequenceNumber);
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "FastKeepAlive error.");
        //}
    }

    private async Task DetachAsync(Subscription target, CancellationToken ct = default)
    {
        if (target.InternalSubscription is { } subscription)
        {
            await subscription.DeleteAsync(true, ct).ConfigureAwait(false);
            if (subscription.Session != null)
            {
                await subscription.Session.RemoveSubscriptionAsync(subscription).ConfigureAwait(false);
            }
            target.InternalSubscription = null;
        }
    }
}
