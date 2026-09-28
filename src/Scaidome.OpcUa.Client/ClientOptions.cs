namespace Scaidome.OpcUa.Client;

public class ClientOptions
{
    public string ApplicationName { get; set; } = "Scaidome";
    public string Url { get; set; } = "opc.tcp://localhost:62541/Quickstarts/ReferenceServer";
    public string? ReverseUrl { get; set; }

    // milliseconds
    public uint SessionTimeout { get; set; } = 60_000;
    public int KeepAliveInterval { get; set; } = 5_000;
    public int ReconnectPeriod { get; set; } = 1_000;
    public int ReconnectPeriodExponentialBackoff { get; set; } = 10_000;

    // security
    public bool UseSecurity { get; set; } = true;
    public bool AutoAccept { get; set; } = true;                // Trust server certificate
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }

    // Certificate-based user identity
    public string? UserCertificateThumbprint { get; set; }      // Thumbprint of user certificate in TrustedUserCertificates store
    public string? UserCertificatePassword { get; set; }        // Password for user certificate private key

    // Application instance certificate password
    public string? CertificatePassword { get; set; }
}
