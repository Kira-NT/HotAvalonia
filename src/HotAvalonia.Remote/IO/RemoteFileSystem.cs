using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HotAvalonia.Net;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for interacting with a remote file system over a network.
/// </summary>
internal sealed partial class RemoteFileSystem : IDisposable
{
    /// <summary>
    /// The root directory for the file system.
    /// </summary>
    private readonly Uri _root;

    /// <summary>
    /// The secret key used for authenticating clients during handshake.
    /// </summary>
    private readonly byte[] _secret;

    /// <summary>
    /// The <see cref="SslTcpListener"/> used to accept client connections.
    /// </summary>
    private readonly SslTcpListener _listener;

    /// <summary>
    /// A collection of active file system watchers.
    /// </summary>
    private readonly ConcurrentDictionary<RemoteFileSystemWatcher, object?> _fileSystemWatchers;

    /// <summary>
    /// The number of active clients being processed by this instance.
    /// </summary>
    private int _clientCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystem"/> class.
    /// </summary>
    /// <param name="root">The root directory for the file system.</param>
    /// <param name="secret">The secret key used for authenticating clients during handshake.</param>
    /// <param name="endpoint">The endpoint on which to listen for incoming connections.</param>
    /// <param name="certificate">The X.509 certificate used for securing communication.</param>
    public RemoteFileSystem(string root, byte[] secret, IPEndPoint endpoint, X509Certificate certificate)
        : this(root, secret, new SslTcpListener(endpoint, certificate))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystem"/> class.
    /// </summary>
    /// <param name="root">The root directory for the file system.</param>
    /// <param name="secret">The secret key used for authenticating clients during handshake.</param>
    /// <param name="endpoint">The endpoint on which to listen for incoming connections.</par
    public RemoteFileSystem(string root, byte[] secret, IPEndPoint endpoint)
        : this(root, secret, new SslTcpListener(endpoint, Name))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystem"/> class.
    /// </summary>
    /// <param name="root">The root directory for the file system.</param>
    /// <param name="secret">The shared secret for handshake authentication.</param>
    /// <param name="listener">The <see cref="SslTcpListener"/> used to accept client connections.</param>
    private RemoteFileSystem(string root, byte[] secret, SslTcpListener listener)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(listener);

        root = Path.GetFullPath(root);
        if (root[^1] != Path.DirectorySeparatorChar && root[^1] != Path.AltDirectorySeparatorChar)
            root += Path.DirectorySeparatorChar;

        _root = new(root);
        _secret = secret;
        _listener = listener;
        _fileSystemWatchers = new();
        _clientCount = 0;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the server should accept shutdown requests from clients.
    /// </summary>
    public bool AllowShutdownRequests { get; set; }

    /// <summary>
    /// Gets or sets the maximum search depth for directory file searches.
    /// </summary>
    /// <remarks>
    /// When set to a value greater than 0, this property limits the number
    /// of file paths returned by methods like <see cref="GetFiles(byte[])"/>.
    /// </remarks>
    public int MaxSearchDepth { get; set; }

    /// <summary>
    /// Gets the number of active clients being processed by this instance.
    /// </summary>
    public int ClientCount => Math.Max(_clientCount, 0);

    /// <inheritdoc cref="RunAsync(int, CancellationToken)"/>
    public Task RunAsync(CancellationToken cancellationToken) => RunAsync(-1, cancellationToken);

    /// <summary>
    /// Starts the remote file system server and begins accepting client connections
    /// until the operation is canceled.
    /// </summary>
    /// <param name="backlog">The maximum number of pending client connections.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAsync(int backlog, CancellationToken cancellationToken)
    {
        if (backlog <= 0)
            _listener.Start();
        else
            _listener.Start(backlog);

        try
        {
            await AcceptClientsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            _listener.Stop();
        }
    }

    /// <summary>
    /// Asynchronously accepts incoming client connections until the operation is canceled.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cancellationTokenSource.Token;

        ConcurrentDictionary<RemoteFileSystemClient, Task> clients = new();
        while (!cancellationToken.IsCancellationRequested)
        {
            RemoteFileSystemClient client = await AcceptClientAsync(cancellationToken);

            Interlocked.Increment(ref _clientCount);
            Task clientTask = Task.Run(() => ProcessClientAsync(client, cancellationToken).ContinueWith(async t =>
            {
                Interlocked.Decrement(ref _clientCount);
                clients.TryRemove(client, out _);
                if (client.ShouldShutdownOnEndOfStream && t.Exception is not null)
                {
                    cancellationTokenSource.Cancel();
                    await client.DisposeAsync();
                }
            }));

            clients[client] = clientTask;
        }

        await Task.WhenAll(clients.Values);
    }

    /// <summary>
    /// Asynchronously accepts an incoming client connection and performs the handshake.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The accepted <see cref="RemoteFileSystemClient"/> instance.</returns>
    private async Task<RemoteFileSystemClient> AcceptClientAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                SslTcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                if (await PerformHandshakeAsync(client.GetStream(), _secret, cancellationToken))
                    return new(client);

                await client.DisposeAsync();
            }
            catch
            {
                continue;
            }
        }
    }

    /// <summary>
    /// Asynchronously performs a handshake with the remote file system client.
    /// </summary>
    /// <param name="stream">The stream associated with the client.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the handshake has been performed successfully; otherwise, <c>false</c>.</returns>
    private static async ValueTask<bool> PerformHandshakeAsync(Stream stream, byte[] secret, CancellationToken cancellationToken)
    {
        try
        {
            ushort id = (ushort)Random.Shared.Next(0, ushort.MaxValue);
            ActionType action = ActionType.PerformHandshake;
            int saltLength = Random.Shared.Next(sizeof(long), 4 * sizeof(long));
            byte[] salt = new byte[saltLength];
            RandomNumberGenerator.Fill(salt);
            await WritePacketAsync(stream, id, action, salt, cancellationToken);

            byte[] handshake = CreateHandshakePacket(secret, salt);
            (ushort clientId, ActionType clientAction, byte[] clientHandshake) = await ReadPacketAsync(stream, cancellationToken);
            if (clientId != id || clientAction != action || !handshake.SequenceEqual(clientHandshake))
                return false;

            ushort nextId = (ushort)(id + 1);
            await WritePacketAsync(stream, nextId, ActionType.KeepAlive, [], cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Asynchronously processes communication with the connected client.
    /// </summary>
    /// <param name="client">The connected client.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    private async Task ProcessClientAsync(RemoteFileSystemClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SslStream stream = client.GetStream();
            (ushort id, ActionType action, byte[] data) = await ReadPacketAsync(stream, cancellationToken);

            try
            {
                bool shouldContinue = await ExecuteActionAsync(client, id, action, data, cancellationToken);
                if (!shouldContinue)
                    return;
            }
            catch (Exception e) when (e is not (EndOfStreamException or OperationCanceledException))
            {
                byte[] exceptionData = RemoteFileSystemException.GetBytes(e);
                await WritePacketAsync(stream, id, ActionType.ThrowException, exceptionData, cancellationToken);
            }
        }

        await client.DisposeAsync();
    }

    /// <summary>
    /// Asynchronously executes the specified action for the connected client using the provided data.
    /// </summary>
    /// <param name="client">The connected client requesting the action.</param>
    /// <param name="id">The unique packet ID for the action.</param>
    /// <param name="action">The action type to execute.</param>
    /// <param name="data">The data payload associated with the action.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// <c>true</c> if the server should continue processing requests from the client;
    /// otherwise, <c>false</c>.
    /// </returns>
    private async ValueTask<bool> ExecuteActionAsync(RemoteFileSystemClient client, ushort id, ActionType action, byte[] data, CancellationToken cancellationToken)
    {
        SslStream stream = client.GetStream();
        switch (action)
        {
            case ActionType.ShutdownOnEndOfStream:
                client.ShouldShutdownOnEndOfStream = AllowShutdownRequests;
                return true;

            case ActionType.CreateFileSystemWatcher:
                RemoteFileSystemWatcher watcher = RemoteFileSystemWatcher.Create(this, client.Client);
                _fileSystemWatchers.TryAdd(watcher, null);
                return false;

            case ActionType.GetFileSystemState:
                byte[] fileSystemState = FileSystemState.Current.ToByteArray();
                await WritePacketAsync(stream, id, action, fileSystemState, cancellationToken);
                return true;

            case ActionType.DirectoryExists:
                byte[] directoryExists = DirectoryExists(data);
                await WritePacketAsync(stream, id, action, directoryExists, cancellationToken);
                return true;

            case ActionType.GetFiles:
                byte[] fileList = GetFiles(data);
                await WritePacketAsync(stream, id, action, fileList, cancellationToken);
                return true;

            case ActionType.FileExists:
                byte[] fileExists = FileExists(data);
                await WritePacketAsync(stream, id, action, fileExists, cancellationToken);
                return true;

            case ActionType.GetLastWriteTimeUtc:
                byte[] writeTime = GetLastWriteTimeUtc(data);
                await WritePacketAsync(stream, id, action, writeTime, cancellationToken);
                return true;

            case ActionType.OpenRead:
                byte[] fileContent = OpenRead(data);
                await WritePacketAsync(stream, id, action, fileContent, cancellationToken);
                return true;

            default:
                return true;
        }
    }

    /// <summary>
    /// Checks if the given path is accessible based on server policies.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path is accessible; otherwise, <c>false</c>.</returns>
    private bool HasAccessTo(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        char lastChar = string.IsNullOrEmpty(path) ? '\0' : path[^1];
        if (lastChar != Path.DirectorySeparatorChar && lastChar != Path.AltDirectorySeparatorChar)
            path += Path.DirectorySeparatorChar;

        if (!Uri.TryCreate(Path.GetFullPath(path), UriKind.Absolute, out Uri? uri))
            return false;

        return _root.IsBaseOf(uri);
    }

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="data">The serialized path information.</param>
    /// <returns>A serialized response indicating whether the directory exists.</returns>
    private byte[] DirectoryExists(byte[] data)
    {
        string path = Encoding.UTF8.GetString(data);
        bool exists = HasAccessTo(path) && Directory.Exists(path);
        return BitConverter.GetBytes(exists);
    }

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="data">The serialized path information.</param>
    /// <returns>A serialized response indicating whether the file exists.</returns>
    private byte[] FileExists(byte[] data)
    {
        string path = Encoding.UTF8.GetString(data);
        bool exists = HasAccessTo(path) && File.Exists(path);
        return BitConverter.GetBytes(exists);
    }

    /// <summary>
    /// Gets the date and time, in Coordinated Universal Time (UTC),
    /// that the specified file or directory was last written to.
    /// </summary>
    /// <param name="data">The serialized path information.</param>
    /// <returns>
    /// A serialized <see cref="DateTime"/> structure set to the date and time that
    /// the specified file or directory was last written to. This value is expressed in UTC time.
    /// </returns>
    private byte[] GetLastWriteTimeUtc(byte[] data)
    {
        string path = Encoding.UTF8.GetString(data);
        DateTime date = HasAccessTo(path) ? File.GetLastAccessTimeUtc(path) : DateTime.MinValue;
        return BitConverter.GetBytes(date.Ticks);
    }

    /// <summary>
    /// Opens a binary file, reads the contents of the file into a byte array,
    /// and then closes the file.
    /// </summary>
    /// <param name="data">The serialized path information.</param>
    /// <returns>A byte array containing the contents of the file.</returns>
    private byte[] OpenRead(byte[] data)
    {
        string path = Encoding.UTF8.GetString(data);
        if (!HasAccessTo(path))
            throw new FileNotFoundException($"Could not find a part of the path '{path}'.", path);

        return File.ReadAllBytes(path);
    }

    /// <summary>
    /// Returns a serialized array of full file names that match a search pattern
    /// in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <remarks>
    /// The number of file paths returned will not exceed the value of
    /// <see cref="MaxSearchDepth"/> unless it is set to a value
    /// less than or equal to 0, in which case there is no limit
    /// on the search depth.
    /// </remarks>
    /// <param name="data">The serialized directory path and search options.</param>
    /// <returns>A serialized byte array containing the file paths.</returns>
    private byte[] GetFiles(byte[] data)
    {
        SearchOption searchOption = (SearchOption)BitConverter.ToInt32(data.AsSpan(0, sizeof(int)));
        int pathByteCount = BitConverter.ToInt32(data.AsSpan(sizeof(int), sizeof(int)));
        int patternByteCount = data.Length - pathByteCount - 2 * sizeof(int);
        string path = Encoding.UTF8.GetString(data, 2 * sizeof(int), pathByteCount);
        string pattern = Encoding.UTF8.GetString(data, data.Length - patternByteCount, patternByteCount);
        if (!HasAccessTo(path))
            throw new DirectoryNotFoundException($"Could not find a part of the path '{path}'.");

        int searchDepth = MaxSearchDepth is int depth && depth > 0 ? depth : int.MaxValue;
        string[] files = Directory.EnumerateFiles(path, pattern, searchOption).Take(searchDepth).ToArray();
        int packetSize = files.Select(static x => Encoding.UTF8.GetByteCount(x) + sizeof(int)).Sum();
        byte[] packet = new byte[packetSize];
        Span<byte> packetSegment = packet;
        foreach (string file in files)
        {
            int fileNameByteCount = Encoding.UTF8.GetBytes(file, packetSegment.Slice(sizeof(int)));
            BitConverter.TryWriteBytes(packetSegment, fileNameByteCount);
            packetSegment = packetSegment.Slice(fileNameByteCount + sizeof(int));
        }

        return packet;
    }

    /// <summary>
    /// Detaches the specified remote file system watcher from this instance.
    /// </summary>
    /// <param name="watcher">The watcher to detach.</param>
    internal void DetachRemoteFileSystemWatcher(RemoteFileSystemWatcher watcher)
        => _fileSystemWatchers.TryRemove(watcher, out _);

    /// <inheritdoc/>
    public void Dispose()
    {
        _listener.Stop();
        foreach (RemoteFileSystemWatcher watcher in _fileSystemWatchers.Keys)
            watcher.Dispose();

        _fileSystemWatchers.Clear();
        _listener.Dispose();
        _secret.AsSpan().Clear();
    }
}
