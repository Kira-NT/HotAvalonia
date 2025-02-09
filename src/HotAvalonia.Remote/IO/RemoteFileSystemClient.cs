using System.Net.Security;
using HotAvalonia.Net;

namespace HotAvalonia.IO;

/// <summary>
/// Provides a client to interact with a remote file system consumer
/// over an SSL/TLS-secured connection.
/// </summary>
internal sealed class RemoteFileSystemClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The <see cref="SslTcpClient"/> used to send and receive data.
    /// </summary>
    private readonly SslTcpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystemClient"/> class.
    /// </summary>
    /// <param name="client">The <see cref="SslTcpClient"/> used to send and receive data.</param>
    public RemoteFileSystemClient(SslTcpClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the client has requested
    /// the server to shut down when the end of the stream is reached.
    /// </summary>
    public bool ShouldShutdownOnEndOfStream { get; set; }

    /// <summary>
    /// Gets the underlying <see cref="SslTcpClient"/> used to send and receive data.
    /// </summary>
    public SslTcpClient Client => _client;

    /// <summary>
    /// Returns the <see cref="SslStream"/> used to send and receive data.
    /// </summary>
    /// <returns>The underlying <see cref="SslStream"/>.</returns>
    public SslStream GetStream() => _client.GetStream();

    /// <inheritdoc/>
    public void Dispose() => _client.Dispose();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
