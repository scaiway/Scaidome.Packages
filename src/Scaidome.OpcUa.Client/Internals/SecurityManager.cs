using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace Scaidome.OpcUa.Client;

/// <summary>
/// Manages OPC UA certificate validation and untrusted certificate acceptance.
/// </summary>
internal sealed class SecurityManager : IDisposable
{
    private readonly ApplicationContext _appContext;
    private readonly ClientOptions _options;
    private readonly ITelemetryContext _telemetry;
    private readonly ILogger<SecurityManager> _logger;
    private bool _disposed;

    public SecurityManager(ApplicationContext appContext, ClientOptions options, ITelemetryContext telemetry)
    {
        _appContext = appContext;
        _options = options;
        _telemetry = telemetry;
        _logger = telemetry.CreateLogger<SecurityManager>();

        //ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
        _appContext.Configuration.CertificateValidator.CertificateValidation += CertificateValidation;
    }

    /// <summary>
    /// Handles the certificate validation event.
    /// This event is triggered every time an untrusted certificate is received from the server.
    /// </summary>
    private void CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
    {
        bool certificateAccepted = false;

        // ****
        // Implement a custom logic to decide if the certificate should be
        // accepted or not and set certificateAccepted flag accordingly.
        // The certificate can be retrieved from the e.Certificate field
        // ***

        ServiceResult error = e.Error;
        _logger.LogWarning("CertificateValidation: {Error}", error);

        if (error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted && _options.AutoAccept)
        {
            certificateAccepted = true;
        }

        if (certificateAccepted)
        {
            _logger.LogWarning("Untrusted Certificate accepted. Subject = {Subject}", e.Certificate.Subject);
            e.Accept = true;
        }
        else
        {
            _logger.LogInformation("Untrusted Certificate rejected. Subject = {Subject}", e.Certificate.Subject);
        }
    }

    /// <summary>
    /// Selects the best endpoint from the server and returns a configured endpoint.
    /// </summary>
    /// <remarks>
    /// Security seen from the clients perspective. The server can still require security.
    /// <list type="bullet">
    /// <item>UseSecurity == true  -> Select endpoint with most robust security, and skip endpoints with no security (e.g. MessageSecurityMode.None)</item>
    /// <item>UseSecurity == false -> Use MessageSecurityMode.None, and no CertificateValidation event is triggered</item>
    /// </list>
    /// </remarks>
    public async Task<ConfiguredEndpoint> SelectEndpointAsync(CancellationToken ct)
    {
        EndpointDescription? endpointDescription = await CoreClientUtils
            .SelectEndpointAsync(
                _appContext.Configuration,
                _options.Url,
                _options.UseSecurity,
                _telemetry,
                ct)
            .ConfigureAwait(false);
        var endpointConfiguration = EndpointConfiguration.Create(_appContext.Configuration);
        return new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
    }

    /// <summary>
    /// Builds an <see cref="IUserIdentity"/> based on the configured security options.
    /// Priority: certificate > username/password > anonymous.
    /// </summary>
    public async Task<IUserIdentity> BuildUserIdentityAsync(CancellationToken ct)
    {
        // Certificate-based user identity
        if (!string.IsNullOrEmpty(_options.UserCertificateThumbprint))
        {
            CertificateTrustList trustedUserCerts = _appContext.Configuration.SecurityConfiguration.TrustedUserCertificates;
            X509Certificate2Collection certs = await trustedUserCerts.GetCertificatesAsync(_telemetry, ct).ConfigureAwait(false);
            X509Certificate2Collection matching = certs.Find(X509FindType.FindByThumbprint, _options.UserCertificateThumbprint, false);

            if (matching.Count == 1)
            {
                var certId = new CertificateIdentifier(matching[0])
                {
                    StorePath = trustedUserCerts.StorePath,
                    StoreType = trustedUserCerts.StoreType
                };

                var passwordProvider = new CertificatePasswordProvider(_options.UserCertificatePassword);
                IUserIdentity identity = await UserIdentity.CreateAsync(certId, passwordProvider, _telemetry, ct).ConfigureAwait(false);

                _logger.LogInformation("Using certificate user identity: {Thumbprint}", _options.UserCertificateThumbprint);
                return identity;
            }

            _logger.LogWarning("User certificate with thumbprint {Thumbprint} not found in TrustedUserCertificates store.", _options.UserCertificateThumbprint);
        }

        // Username/password identity
        if (_options.UserName != null)
        {
            _logger.LogInformation("Using username identity: {UserName}", _options.UserName);
            return new UserIdentity(_options.UserName, Encoding.UTF8.GetBytes(_options.UserPassword ?? string.Empty));
        }

        // Anonymous
        return new UserIdentity();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _appContext.Configuration.CertificateValidator.CertificateValidation -= CertificateValidation;
        _disposed = true;
    }

    // TODO

    /// <summary>
    /// A dialog which asks for user input on untrusted certificates.
    /// </summary>
    private sealed class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string _message = string.Empty;
        private bool _ask;

        public override void Message(string text, bool ask = false)
        {
            _message = text;
            _ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (_ask)
            {
                var message = new StringBuilder(_message);
                message.Append(" (y/n, default y): ");
                Console.Write(message.ToString());

                try
                {
                    ConsoleKeyInfo result = Console.ReadKey();
                    Console.WriteLine();
                    return await Task.FromResult(result.KeyChar is 'y' or 'Y' or '\r').ConfigureAwait(false);
                }
                catch
                {
                    // intentionally fall through
                }
            }
            else
            {
                Console.WriteLine(_message);
            }

            return await Task.FromResult(true).ConfigureAwait(false);
        }
    }
}
