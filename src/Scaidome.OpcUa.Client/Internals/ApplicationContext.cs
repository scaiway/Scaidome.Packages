using Opc.Ua;
using Opc.Ua.Configuration;

namespace Scaidome.OpcUa.Client;

/// <summary>
/// Holds the initialized OPC UA application instance and its loaded configuration.
/// Use <see cref="CreateAsync"/> to build a validated context.
/// </summary>
internal class ApplicationContext
{
    private ApplicationContext(ApplicationInstance application, ApplicationConfiguration configuration)
    {
        Application = application;
        Configuration = configuration;
    }

    public ApplicationInstance Application { get; init; }
    public ApplicationConfiguration Configuration { get; init; }

    /// <summary>
    /// Creates and validates an application context by loading the configuration
    /// and checking the application instance certificates.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the application certificate is invalid. The message says why and which folders to delete to get a new one.
    /// </exception>
    /// <param name="applicationName">The application name.</param>
    /// <param name="configSectionName">The configuration section name (matches the XML config filename stem).</param>
    /// <param name="telemetry">The telemetry context for logging and diagnostics.</param>
    /// <param name="certificatePassword">Optional password for the private key certificate.</param>
    /// <param name="renewCertificate">If true, deletes and recreates the application instance certificate.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<ApplicationContext> CreateAsync(
        string applicationName,
        string configSectionName,
        ITelemetryContext telemetry,
        string? certificatePassword = null,
        bool renewCertificate = false,
        CancellationToken ct = default)
    {
        var application = new ApplicationInstance(telemetry)
        {
            ApplicationName = applicationName,
            ApplicationType = ApplicationType.Client,
            ConfigSectionName = configSectionName,
            CertificatePasswordProvider = new CertificatePasswordProvider(certificatePassword)
        };

        ApplicationConfiguration config = await application.LoadApplicationConfigurationAsync(silent: false).ConfigureAwait(false);

        if (renewCertificate)
        {
            await application.DeleteApplicationInstanceCertificateAsync().ConfigureAwait(false);
        }

        bool haveAppCertificate;
        try
        {
            haveAppCertificate = await application.CheckApplicationInstanceCertificatesAsync(false).ConfigureAwait(false);
        }
        catch (ServiceResultException ex)
        {
            // The SDK only says "invalid"; tell the user why and which files to delete to get a new certificate.
            throw new InvalidOperationException(await DescribeInvalidCertificateAsync(config, telemetry, ct).ConfigureAwait(false), ex);
        }

        if (!haveAppCertificate)
        {
            throw new InvalidOperationException("Application instance certificate invalid!");
        }

        return new ApplicationContext(application, config);
    }

    private static async Task<string> DescribeInvalidCertificateAsync(ApplicationConfiguration config, ITelemetryContext telemetry, CancellationToken ct)
    {
        var reasons = new List<string>();
        var folders = new List<string>();

        foreach (var id in config.SecurityConfiguration.ApplicationCertificates)
        {
            if (string.IsNullOrEmpty(id.StorePath))
                continue;

            var storePath = Path.GetFullPath(Utils.ReplaceSpecialFolderNames(id.StorePath));
            folders.Add(Path.Combine(storePath, "certs"));
            folders.Add(Path.Combine(storePath, "private"));

            // No applicationUri filter: the certificate we're looking for is typically one whose URI doesn't match.
            var certificate = await id.FindAsync(false, null, telemetry, ct).ConfigureAwait(false);
            if (certificate is null)
                continue;

            var uris = X509Utils.GetApplicationUrisFromCertificate(certificate);
            if (!uris.Contains(config.ApplicationUri, StringComparer.Ordinal))
                reasons.Add($"its application URI is '{string.Join("', '", uris)}', but the configuration expects '{config.ApplicationUri}'");

            if (certificate.NotAfter < DateTime.Now)
                reasons.Add($"it expired on {certificate.NotAfter:yyyy-MM-dd}");
            else if (certificate.NotBefore > DateTime.Now)
                reasons.Add($"it is not valid until {certificate.NotBefore:yyyy-MM-dd}");
        }

        var reason = reasons.Count > 0
            ? $"The client certificate is invalid: {string.Join("; ", reasons.Distinct())}."
            : "The client certificate is invalid. See the log for details.";

        return reason + Environment.NewLine + Environment.NewLine
            + "To create a new certificate, delete the files in these folders and connect again:" + Environment.NewLine
            + string.Join(Environment.NewLine, folders.Distinct(StringComparer.OrdinalIgnoreCase).Select(f => "  " + f)) + Environment.NewLine + Environment.NewLine
            + "Servers that trusted the old certificate must then trust the new one.";
    }
}
