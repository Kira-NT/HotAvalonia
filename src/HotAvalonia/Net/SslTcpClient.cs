using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace HotAvalonia.Net;

/// <summary>
/// Provides SSL/TLS communication functionality over a TCP connection.
/// </summary>
internal sealed class SslTcpClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The underlying TCP client used for network communication.
    /// </summary>
    private readonly TcpClient _client;

    /// <summary>
    /// The <see cref="SslStream"/> used for secure communication over the TCP connection.
    /// </summary>
    private readonly SslStream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="SslTcpClient"/> class.
    /// </summary>
    /// <param name="client">The <see cref="TcpClient"/> used for network communication.</param>
    /// <param name="stream">The <see cref="SslStream"/> used for secure communication.</param>
    private SslTcpClient(TcpClient client, SslStream stream)
    {
        _client = client;
        _stream = stream;
    }

    /// <summary>
    /// Authenticates the server side of an SSL/TLS connection.
    /// </summary>
    /// <param name="client">The TCP client for the connection.</param>
    /// <param name="certificate">The server's SSL certificate.</param>
    /// <returns>An authenticated <see cref="SslTcpClient"/> instance.</returns>
    public static SslTcpClient AuthenticateAsServer(TcpClient client, X509Certificate certificate)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        _ = certificate ?? throw new ArgumentNullException(nameof(certificate));

        SslStream sslStream = new(client.GetStream(), false);
        SslTcpClient sslClient = new(client, sslStream);
        try
        {
            sslStream.AuthenticateAsServer(certificate);
            Debug.Assert(sslStream.IsAuthenticated);
            return sslClient;
        }
        catch
        {
            sslClient.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Asynchronously authenticates the server side of an SSL/TLS connection.
    /// </summary>
    /// <param name="client">The TCP client for the connection.</param>
    /// <param name="certificate">The server's SSL certificate.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An authenticated <see cref="SslTcpClient"/> instance.</returns>
    public static async Task<SslTcpClient> AuthenticateAsServerAsync(TcpClient client, X509Certificate certificate, CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        _ = certificate ?? throw new ArgumentNullException(nameof(certificate));

        SslStream sslStream = new(client.GetStream(), false);
        SslTcpClient sslClient = new(client, sslStream);
        try
        {
#if NETSTANDARD2_0
            cancellationToken.ThrowIfCancellationRequested();
            await sslStream.AuthenticateAsServerAsync(certificate).ConfigureAwait(false);
#else
            SslServerAuthenticationOptions options = new() { ServerCertificate = certificate };
            await sslStream.AuthenticateAsServerAsync(options, cancellationToken).ConfigureAwait(false);
#endif
            Debug.Assert(sslStream.IsAuthenticated);
            return sslClient;
        }
        catch
        {
            await sslClient.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Authenticates the client side of an SSL/TLS connection.
    /// </summary>
    /// <param name="client">The TCP client for the connection.</param>
    /// <param name="targetHost">The target host for server authentication.</param>
    /// <returns>An authenticated <see cref="SslTcpClient"/> instance.</returns>
    public static SslTcpClient AuthenticateAsClient(TcpClient client, string targetHost)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        _ = targetHost ?? throw new ArgumentNullException(nameof(targetHost));

        SslStream sslStream = new(client.GetStream(), false, ValidateRemoteCertificate, null);
        SslTcpClient sslClient = new(client, sslStream);
        try
        {
            sslStream.AuthenticateAsClient(targetHost);
            Debug.Assert(sslStream.IsAuthenticated);
            return sslClient;
        }
        catch
        {
            sslClient.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Asynchronously authenticates the client side of an SSL/TLS connection.
    /// </summary>
    /// <param name="client">The TCP client for the connection.</param>
    /// <param name="targetHost">The target host for server authentication.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An authenticated <see cref="SslTcpClient"/> instance.</returns>
    public static async Task<SslTcpClient> AuthenticateAsClientAsync(TcpClient client, string targetHost, CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        _ = targetHost ?? throw new ArgumentNullException(nameof(targetHost));

        SslStream sslStream = new(client.GetStream(), false, ValidateRemoteCertificate, null);
        SslTcpClient sslClient = new(client, sslStream);
        try
        {
#if NETSTANDARD2_0
            cancellationToken.ThrowIfCancellationRequested();
            await sslStream.AuthenticateAsClientAsync(targetHost).ConfigureAwait(false);
#else
            SslClientAuthenticationOptions options = new() { TargetHost = targetHost };
            await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
#endif
            Debug.Assert(sslStream.IsAuthenticated);
            return sslClient;
        }
        catch
        {
            await sslClient.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Validates the remote certificate provided during the SSL/TLS handshake.
    /// </summary>
    /// <param name="sender">The sender object of the validation request.</param>
    /// <param name="certificate">The remote party's certificate.</param>
    /// <param name="chain">The certificate chain associated with the remote party.</param>
    /// <param name="errors">The SSL policy errors detected during the validation.</param>
    /// <returns><c>true</c> if the certificate is valid; otherwise, <c>false</c>.</returns>
    private static bool ValidateRemoteCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (sender is null || certificate is null)
            return false;

        // We want to allow self-signed certificates.
        // Thus, don't treat `UntrustedRoot` as an error condition.
        if (errors is SslPolicyErrors.RemoteCertificateChainErrors)
            return chain?.ChainStatus.All(static x => x.Status is X509ChainStatusFlags.NoError or X509ChainStatusFlags.UntrustedRoot) ?? false;

        return errors is SslPolicyErrors.None;
    }

    /// <summary>
    /// Gets the amount of data that has been received from the network and is available to be read.
    /// </summary>
    public int Available => _client.Available;

    /// <summary>
    /// Gets or sets the underlying <see cref="Socket"/>.
    /// </summary>
    public Socket Client
    {
        get => _client.Client;
        set => _client.Client = value;
    }

    /// <summary>
    /// Gets a value indicating whether the underlying <see cref="Socket"/>
    /// for a <see cref="SslTcpClient"/> is connected to a remote host.
    /// </summary>
    public bool Connected => _client.Connected;

    /// <summary>
    /// Gets or sets a value that specifies whether the <see cref="SslTcpClient"/>
    /// allows only one client to use a port.
    /// </summary>
    public bool ExclusiveAddressUse
    {
        get => _client.ExclusiveAddressUse;
        set => _client.ExclusiveAddressUse = value;
    }

    /// <summary>
    /// Gets or sets information about the linger state of the associated socket.
    /// </summary>
    public LingerOption? LingerState
    {
        get => _client.LingerState;
        set => _client.LingerState = value!;
    }

    /// <summary>
    /// Gets or sets a value that disables a delay when send or receive buffers are not full.
    /// </summary>
    public bool NoDelay
    {
        get => _client.NoDelay;
        set => _client.NoDelay = value;
    }

    /// <summary>
    /// Gets or sets the size of the receive buffer.
    /// </summary>
    public int ReceiveBufferSize
    {
        get => _client.ReceiveBufferSize;
        set => _client.ReceiveBufferSize = value;
    }

    /// <summary>
    /// Gets or sets the amount of time a <see cref="SslTcpClient"/>
    /// will wait to receive data once a read operation is initiated.
    /// </summary>
    public int ReceiveTimeout
    {
        get => _client.ReceiveTimeout;
        set => _client.ReceiveTimeout = value;
    }

    /// <summary>
    /// Gets or sets the size of the send buffer.
    /// </summary>
    public int SendBufferSize
    {
        get => _client.SendBufferSize;
        set => _client.SendBufferSize = value;
    }

    /// <summary>
    /// Gets or sets the amount of time a <see cref="SslTcpClient"/>
    /// will wait for a send operation to complete successfully.
    /// </summary>
    public int SendTimeout
    {
        get => _client.SendTimeout;
        set => _client.SendTimeout = value;
    }

    /// <summary>
    /// Returns the <see cref="SslStream"/> used to send and receive data.
    /// </summary>
    /// <returns>The underlying <see cref="SslStream"/>.</returns>
    public SslStream GetStream() => _stream;

    /// <summary>
    /// Releases the managed and unmanaged resources
    /// used by the <see cref="SslTcpClient"/>.
    /// </summary>
    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }

    /// <summary>
    /// Asynchronously releases the managed and unmanaged resources
    /// used by the <see cref="SslTcpClient"/>.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
#if NETSTANDARD2_0
        await Task.CompletedTask;
        _stream.Dispose();
#else
        await _stream.DisposeAsync().ConfigureAwait(false);
#endif
        _client.Dispose();
    }
}
