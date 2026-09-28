using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using OpcSubscription = Opc.Ua.Client.Subscription;

namespace Scaidome.OpcUa.Client;

internal partial class Client : IClient
{
    private readonly Lock _lock = new();
    private readonly ApplicationContext _appContext;
    private readonly SecurityManager _securityManager;
    private readonly ClientOptions _options;

    private readonly HostTelemetry _telemetry;
    private readonly ILogger<Client> _logger;
    private readonly Meter _meter;
    private readonly Counter<long> _reconnectCounter;

    private ISession? _session;
    private SessionReconnectHandler? _reconnectHandler;
    private volatile ClientState _state = ClientState.Disconnected;
    private bool _disposed;

    public Client(
        ApplicationContext appContext,
        SecurityManager securityManager,
        ClientOptions options,
        HostTelemetry telemetry)
    {
        _appContext = appContext;
        _securityManager = securityManager;
        _options = options;
        _telemetry = telemetry;
        _logger = telemetry.CreateLogger<Client>();
        _meter = telemetry.CreateMeter();
        _reconnectCounter = _meter.CreateCounter<long>("scai.opcua.client.reconnects", description: "Number of completed session reconnects");
    }

    public string Url => _options.Url;


    /// <summary>
    /// Connects to the OPC UA server, creates a session, and subscribes to the configured nodes.
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using Activity? activity = _telemetry.StartActivity();
        try
        {
            if (_session != null && _session.Connected)
            {
                _logger.LogInformation("Session already connected.");
                return true;
            }

            _logger.LogInformation("Connecting to {ServerUrl}...", _options.Url);

            // Transport security
            ConfiguredEndpoint endpoint = await _securityManager.SelectEndpointAsync(ct).ConfigureAwait(false);
            // User identity: certificate > username/password > anonymous
            IUserIdentity userIdentity = await _securityManager.BuildUserIdentityAsync(ct).ConfigureAwait(false);

            // Triggers CertificateValidation event for untrusted server certs
            var sessionFactory = new DefaultSessionFactory(_telemetry);
            ISession session = await sessionFactory
                .CreateAsync(
                    _appContext.Configuration,
                    endpoint,
                    true,   // updateBeforeConnect
                    false,  // checkDomain
                    _appContext.Configuration.ApplicationName,
                    _options.SessionTimeout,
                    userIdentity,
                    null,   // preferredLocales
                    ct)
                .ConfigureAwait(false);

            if (session == null || !session.Connected)
            {
                _logger.LogError("Failed to create session.");
                return false;
            }

            _session = session;

            // Configure keep-alive and reconnection
            _session.KeepAliveInterval = _options.KeepAliveInterval;
            _session.DeleteSubscriptionsOnClose = false;
            _session.TransferSubscriptionsOnReconnect = true;
            _session.MinPublishRequestCount = 3;
            _session.KeepAlive += OnKeepAlive;
            _state = ClientState.Connected;

            _reconnectHandler = new SessionReconnectHandler(_telemetry, true, _options.ReconnectPeriodExponentialBackoff);
            _logger.LogInformation("Session created: {SessionName}", _session.SessionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connect error.");
            return false;
        }
    }

    /// <summary>
    /// Disconnects the session from the server.
    /// </summary>
    public async Task DisconnectAsync(bool leaveChannelOpen = false)
    {
        try
        {
            if (_session == null)
            {
                return;
            }

            _logger.LogInformation("Disconnecting...");
            await RemoveAllSubscriptions().ConfigureAwait(false); // TODO: check if reference impl does this

            lock (_lock)
            {
                _state = ClientState.Disconnected;
            _session.KeepAlive -= OnKeepAlive;
                _reconnectHandler?.Dispose();
                _reconnectHandler = null;
            }

            await _session.CloseAsync(!leaveChannelOpen).ConfigureAwait(false);
            if (leaveChannelOpen)
            {
                _session.DetachChannel();
            }
            _session.Dispose();
            _session = null;

            _logger.LogInformation("Session disconnected.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disconnect error.");
        }
    }


    public ClientState State => _state;

    private void OnKeepAlive(ISession session, KeepAliveEventArgs e)
    {
        try
        {
            if (_session == null || !_session.Equals(session))
            {
                return;
            }

            if (ServiceResult.IsBad(e.Status))
            {
                _state = ClientState.Reconnecting;
                var state = _reconnectHandler!.BeginReconnect(_session, null, _options.ReconnectPeriod, OnReconnectComplete);
                _logger.LogInformation("KeepAlive status {StatusCode}, reconnect state {State}.", e.Status, state);
                e.CancelKeepAlive = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KeepAlive error.");
        }
    }

    private void OnReconnectComplete(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _reconnectHandler))
        {
            return;
        }

        _reconnectCounter.Add(1, new TagList { { "server.url", _options.Url } });
        _state = ClientState.Connected;
        lock (_lock)
        {
            if (_reconnectHandler?.Session != null)
            {
                if (!ReferenceEquals(_session, _reconnectHandler.Session))
                {
                    _logger.LogInformation("Reconnected to new session: {SessionId}", _reconnectHandler.Session.SessionId);
                    ISession? old = _session;
                    _session = _reconnectHandler.Session;
                    Utils.SilentDispose(old);
                }
                else
                {
                    _logger.LogInformation("Reactivated session: {SessionId}", _reconnectHandler.Session.SessionId);
                }
            }
            else
            {
                _logger.LogInformation("KeepAlive recovered.");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Utils.SilentDispose(_session);
        _reconnectHandler?.Dispose();
        _securityManager.Dispose();
        _meter.Dispose();
        _telemetry.Dispose();
        _disposed = true;
    }
}
