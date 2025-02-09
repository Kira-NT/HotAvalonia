using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HotAvalonia.Net;

/// <summary>
/// Represents a TCP listener that secures client connections using SSL/TLS.
/// </summary>
internal sealed class SslTcpListener : IDisposable
{
    /// <summary>
    /// The underlying <see cref="TcpListener"/> used for communication.
    /// </summary>
    private readonly TcpListener _listener;

    /// <summary>
    /// The X.509 certificate used for securing communication.
    /// </summary>
    private readonly X509Certificate _certificate;

    /// <summary>
    /// Initializes a new instance of the <see cref="SslTcpListener"/> class
    /// using a specified local endpoint and generates a self-signed certificate.
    /// </summary>
    /// <param name="localEndpoint">The local IP endpoint on which the listener will bind.</param>
    /// <param name="hostname">The hostname to be included in the generated self-signed certificate.</param>
    public SslTcpListener(IPEndPoint localEndpoint, string hostname)
        : this(localEndpoint, GenerateCertificate(hostname: hostname))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SslTcpListener"/> class
    /// using a specified local endpoint and SSL certificate.
    /// </summary>
    /// <param name="localEndpoint">The local IP endpoint on which the listener will bind.</param>
    /// <param name="certificate">The X.509 certificate used for securing communication.</param>
    public SslTcpListener(IPEndPoint localEndpoint, X509Certificate certificate)
    {
        ArgumentNullException.ThrowIfNull(localEndpoint);
        ArgumentNullException.ThrowIfNull(certificate);

        _listener = new(localEndpoint);
        _certificate = certificate;
    }

    /// <summary>
    /// Gets the X.509 certificate used for securing communication.
    /// </summary>
    public X509Certificate Certificate => _certificate;

    /// <summary>
    /// Gets the underlying network <see cref="Socket"/>.
    /// </summary>
    public Socket Server => _listener.Server;

    /// <summary>
    /// Gets the underlying <see cref="EndPoint"/> of the current <see cref="SslTcpListener"/>.
    /// </summary>
    public EndPoint LocalEndpoint => _listener.LocalEndpoint;

    /// <summary>
    /// Gets or sets a value that specifies whether the <see cref="SslTcpListener"/>
    /// allows only one underlying socket to listen to a specific port.
    /// </summary>
    public bool ExclusiveAddressUse
    {
        get => _listener.ExclusiveAddressUse;
        set => _listener.ExclusiveAddressUse = value;
    }

    /// <summary>
    /// Determines if there are pending connection requests.
    /// </summary>
    /// <returns><c>true</c> if connections are pending; otherwise, <c>false</c>.</returns>
    public bool Pending() => _listener.Pending();

    /// <summary>
    /// Starts listening for incoming connection requests with a maximum number of pending connection.
    /// </summary>
    /// <param name="backlog">The maximum length of the pending connections queue.</param>
    public void Start(int backlog) => _listener.Start(backlog);

    /// <summary>
    /// Starts listening for incoming connection requests.
    /// </summary>
    public void Start() => _listener.Start();

    /// <summary>
    /// Closes the listener.
    /// </summary>
    public void Stop() => _listener.Stop();

    /// <summary>
    /// Releases all resources used by the current <see cref="SslTcpListener"/> instance.
    /// </summary>
    public void Dispose()
    {
        _listener.Stop();
        _certificate.Dispose();
    }

    /// <summary>
    /// Accepts a pending connection request.
    /// </summary>
    /// <returns>A <see cref="SslTcpClient"/> used to send and receive data.</returns>
    public SslTcpClient AcceptTcpClient()
    {
        TcpClient client = _listener.AcceptTcpClient();
        return SslTcpClient.AuthenticateAsServer(client, _certificate);
    }

    /// <summary>
    /// Asynchronously accepts a pending connection request.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="SslTcpClient"/> used to send and receive data.</returns>
    public async Task<SslTcpClient> AcceptTcpClientAsync(CancellationToken cancellationToken = default)
    {
        TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        return await SslTcpClient.AuthenticateAsServerAsync(client, _certificate, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a self-signed SSL/TLS certificate.
    /// </summary>
    /// <param name="oid">The optional object identifier (OID) for the ECC curve. Defaults to P-256 ("1.2.840.10045.3.1.7").</param>
    /// <param name="hostname">The hostname to include in the certificate subject. Defaults to "Self-Signed".</param>
    /// <param name="notBefore">The start date of the certificate validity period. Defaults to one day before the current date.</param>
    /// <param name="notAfter">The end date of the certificate validity period. Defaults to seven days from the current date.</param>
    /// <returns>A self-signed <see cref="X509Certificate"/>.</returns>
    private static X509Certificate2 GenerateCertificate(string? oid = null, string? hostname = null, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
    {
        oid ??= "1.2.840.10045.3.1.7";
        hostname ??= "Self-Signed";
        notBefore ??= DateTimeOffset.UtcNow.AddDays(-1);
        notAfter ??= DateTimeOffset.UtcNow.AddDays(7);

        string subject = $"CN={hostname}";
        using ECDsa key = ECDsa.Create(ECCurve.CreateFromValue(oid));
        CertificateRequest request = new(subject, key, HashAlgorithmName.SHA256)
        {
            CertificateExtensions = { new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true) },
        };

        using X509Certificate2 certificate = request.CreateSelfSigned(notBefore.Value, notAfter.Value);
        byte[] pfxData = certificate.Export(X509ContentType.Pfx);
#if NET9_0_OR_GREATER
        X509Certificate2 pfxCertificate = X509CertificateLoader.LoadPkcs12(pfxData, null);
#else
        X509Certificate2 pfxCertificate = new(pfxData);
#endif
        return pfxCertificate;
    }
}
