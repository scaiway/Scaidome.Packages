using System.Threading.Channels;

namespace Scaidome.OpcUa.Client;

public record SubscriptionOptions
{
    public required string DisplayName { get; init; }
    public bool PublishingEnabled { get; init; } = true;
    public int PublishingInterval { get; init; } = 1000;
    public uint KeepAliveCount { get; init; } = 10;                 // Number of publishing intervals without data before server sends a keep-alive message. Ensures client knows subscription is still active
    public uint MinLifetimeInterval { get; init; } = 120_000;       // Used to calculate minimum LifetimeCount. Should be larger than session timeout
    public int SamplingInterval { get; init; } = 500;               // Node sampling interval
    public uint QueueSize { get; init; } = 10;                      // If SamplingInterval < PublishingInterval values are queued
    public bool SequentialPublishing { get; init; } = true;         // Ensures notifications are processed in order
    public bool DisableMonitoredItemCache { get; init; } = true;    // Disables caching of last values (reduces memory, improves performance with FastDataChangeCallback)
    public Action<DataValue[]>? OnDataChanged { get; init; }                             // Per-subscription data change callback.
    public ChannelWriter<DataValueChangedMessage>? OnDataChangedChannel { get; init; } // Per-subscription data change channel. When either is set, overrides the client-level handlers for this subscription.
}
