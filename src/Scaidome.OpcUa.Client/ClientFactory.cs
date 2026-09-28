using Microsoft.Extensions.Logging;

namespace Scaidome.OpcUa.Client;

public static class ClientFactory
{
    const string configSectionName = "Scaidome.OpcUa.Client";

    public static async Task<IClient> CreateAsync(
        ClientOptions options,
        ILoggerFactory loggerFactory)
    {
        // The client owns the telemetry context and disposes it with itself.
        var telemetry = new HostTelemetry(loggerFactory);
        try
        {
            var appContext = await ApplicationContext
                .CreateAsync(
                    options.ApplicationName,
                    configSectionName,
                    telemetry,
                    options.CertificatePassword)
                .ConfigureAwait(false);

            var security = new SecurityManager(appContext, options, telemetry);

            return new Client(appContext, security, options, telemetry);
        }
        catch
        {
            telemetry.Dispose();
            throw;
        }
    }
}
