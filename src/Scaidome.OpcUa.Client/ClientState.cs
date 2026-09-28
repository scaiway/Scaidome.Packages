namespace Scaidome.OpcUa.Client;

/// <summary>Where the client's session is, as the keepalive and the reconnect handler see it.</summary>
/// <remarks>
/// The client has always known this — <c>OnKeepAlive</c> and <c>OnReconnectComplete</c> fire on exactly these transitions — it
/// just had nowhere to put it and only logged. Three states rather than four: there is no error-rate window here, so nothing
/// can be Degraded, and a push source has no per-request outcomes to build one from.
/// </remarks>
public enum ClientState
{
    /// <summary>No session. Never connected, disconnected, or a connect that failed.</summary>
    Disconnected,

    /// <summary>Session live and the keepalive is good.</summary>
    Connected,

    /// <summary>The keepalive went bad and <c>SessionReconnectHandler</c> is retrying with its own backoff.</summary>
    Reconnecting,
}
