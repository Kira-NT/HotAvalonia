using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using HotAvalonia.Helpers;
using HotAvalonia.Net;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for interacting with a remote file system over a network.
/// </summary>
internal sealed partial class RemoteFileSystem : IFileSystem
{
    /// <summary>
    /// The factory function used to create authenticated TCP clients.
    /// </summary>
    private readonly Func<CancellationToken, Task<SslTcpClient>> _clientFactory;

    /// <summary>
    /// The TCP client used for communication with the remote file system.
    /// </summary>
    private readonly SslTcpClient _client;

    /// <summary>
    /// The state of the remote file system.
    /// </summary>
    private readonly FileSystemState _fileSystemState;

    /// <summary>
    /// A dictionary mapping IDs of active requests to their completion sources.
    /// </summary>
    private ConcurrentDictionary<int, TaskCompletionSource<byte[]>> _requests;

    /// <summary>
    /// A semaphore used to ensure exclusive write access to the network stream.
    /// </summary>
    private readonly SemaphoreSlim _writeLock;

    /// <summary>
    /// The current request ID.
    /// </summary>
    private ushort _currentId;

    /// <summary>
    /// The cancellation token source used to stop the read loop.
    /// </summary>
    private CancellationTokenSource? _readLoopCancellationTokenSource;

    /// <summary>
    /// The timeout value for synchronous operations, in milliseconds.
    /// </summary>
    private int _timeout;

    /// <summary>
    /// The default timeout value, in milliseconds.
    /// </summary>
    private const int DefaultTimeout = 10000;

    /// <summary>
    /// The minimum allowed timeout value, in milliseconds.
    /// </summary>
    private const int MinTimeout = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystem"/> class.
    /// </summary>
    /// <param name="client">The TCP client used for communication with the remote file system.</param>
    /// <param name="clientFactory">The factory function used to create authenticated TCP clients.</param>
    /// <param name="fileSystemState">The initial state of the file system.</param>
    private RemoteFileSystem(SslTcpClient client, Func<CancellationToken, Task<SslTcpClient>> clientFactory, FileSystemState fileSystemState)
    {
        _client = client;
        _clientFactory = clientFactory;
        _fileSystemState = fileSystemState;
        _requests = new();
        _writeLock = new(1, 1);
        _currentId = 0;
        _readLoopCancellationTokenSource = null;
        _timeout = DefaultTimeout;
    }

    /// <summary>
    /// Connects to a remote file system and initializes it.
    /// </summary>
    /// <param name="endpoint">The endpoint to connect to.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The connected <see cref="RemoteFileSystem"/>.</returns>
    public static async Task<RemoteFileSystem> ConnectAsync(IPEndPoint endpoint, byte[] secret, CancellationToken cancellationToken = default)
    {
        Task<SslTcpClient> CreateClientAsync(CancellationToken ct) => AuthenticateClientAsync(endpoint, secret, ct);
        SslTcpClient client = await CreateClientAsync(cancellationToken).ConfigureAwait(false);
        SslStream stream = client.GetStream();

        await WritePacketAsync(stream, 0, ActionType.ShutdownOnEndOfStream, [], cancellationToken).ConfigureAwait(false);
        await WritePacketAsync(stream, 0, ActionType.GetFileSystemState, [], cancellationToken).ConfigureAwait(false);
        FileSystemState state = FileSystemState.FromByteArray((await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false)).Data);

        RemoteFileSystem fileSystem = new(client, CreateClientAsync, state);
        fileSystem.Start();
        return fileSystem;
    }

    /// <summary>
    /// Creates and authenticates a TCP client used for communication with the remote file system.
    /// </summary>
    /// <param name="endpoint">The endpoint to connect to.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An authenticated <see cref="SslTcpClient"/>.</returns>
    private static async Task<SslTcpClient> AuthenticateClientAsync(IPEndPoint endpoint, byte[] secret, CancellationToken cancellationToken = default)
    {
        TcpClient client = new();
        await client.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);

        SslTcpClient sslClient = await SslTcpClient.AuthenticateAsClientAsync(client, Name, cancellationToken).ConfigureAwait(false);
        SslStream sslStream = sslClient.GetStream();
        await PerformHandshakeAsync(sslStream, secret, cancellationToken).ConfigureAwait(false);

        return sslClient;
    }

    /// <summary>
    /// Performs a handshake with the remote file system.
    /// </summary>
    /// <param name="stream">The stream used for communication with the remote file system.</param>
    /// <param name="secret">The secret key used for authentication.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous handshake operation.</returns>
    private static async Task PerformHandshakeAsync(Stream stream, byte[] secret, CancellationToken cancellationToken = default)
    {
        (ushort id, ActionType action, byte[] salt) = await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false);
        if (action is not ActionType.PerformHandshake)
            throw new ProtocolViolationException("The server refused to perform a handshake.");

        byte[] handshake = CreateHandshakePacket(secret, salt);
        await WritePacketAsync(stream, id, action, handshake, cancellationToken).ConfigureAwait(false);

        (ushort nextId, ActionType nextAction, byte[] data) = await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false);
        if (nextId != (ushort)(id + 1) || nextAction is not ActionType.KeepAlive || data?.Length != 0)
            throw new ProtocolViolationException("The server did not indicate whether the handshake was successful.");
    }

    /// <inheritdoc/>
    public StringComparer PathComparer => _fileSystemState.PathComparer;

    /// <inheritdoc/>
    public StringComparison PathComparison => _fileSystemState.PathComparison;

    /// <inheritdoc/>
    public char DirectorySeparatorChar => _fileSystemState.DirectorySeparatorChar;

    /// <inheritdoc/>
    public char AltDirectorySeparatorChar => _fileSystemState.AltDirectorySeparatorChar;

    /// <inheritdoc/>
    public char VolumeSeparatorChar => _fileSystemState.VolumeSeparatorChar;

    /// <summary>
    /// Gets or sets the length of time, in milliseconds, before a synchronous operation times out.
    /// </summary>
    public int Timeout
    {
        get => _timeout;
        set => _timeout = value is > 0 and < MinTimeout ? MinTimeout : value;
    }

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher()
    {
        using CancellationTokenSource? tokenSource = _timeout > 0 ? new(_timeout) : null;
        CancellationToken cancellationToken = tokenSource?.Token ?? default;
        return CreateFileSystemWatcherAsync(cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask<IFileSystemWatcher> CreateFileSystemWatcherAsync(CancellationToken cancellationToken = default)
    {
        SslTcpClient client = await _clientFactory(cancellationToken).ConfigureAwait(false);
        SslStream stream = client.GetStream();

        await WritePacketAsync(stream, 0, ActionType.CreateFileSystemWatcher, [], cancellationToken).ConfigureAwait(false);
        return RemoteFileSystemWatcher.Create(this, client);
    }

    /// <inheritdoc/>
    public bool DirectoryExists([NotNullWhen(true)] string? path)
    {
        using CancellationTokenSource? tokenSource = _timeout > 0 ? new(_timeout) : null;
        CancellationToken cancellationToken = tokenSource?.Token ?? default;
        return DirectoryExistsAsync(path, cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(ActionType.DirectoryExists, request, cancellationToken).ConfigureAwait(false);
        return BitConverter.ToBoolean(response);
    }

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        using CancellationTokenSource? tokenSource = _timeout > 0 ? new(_timeout) : null;
        CancellationToken cancellationToken = tokenSource?.Token ?? default;
        return GetFilesAsync(path, searchPattern, searchOption, cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IEnumerable<string> files = await GetFilesAsync(path, searchPattern, searchOption, cancellationToken).ConfigureAwait(false);
        foreach (string file in files)
            yield return file;
    }

    /// <summary>
    /// Asynchronously returns an enumerable collection of full file names that match a search pattern
    /// in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/> and that match the specified search pattern and search option.
    /// </returns>
    private async Task<IEnumerable<string>> GetFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(searchPattern);

        int pathByteCount = Encoding.UTF8.GetByteCount(path);
        int searchPatternByteCount = Encoding.UTF8.GetByteCount(searchPattern);
        int requestLength = pathByteCount + searchPatternByteCount + 2 * sizeof(int);
        byte[] request = new byte[requestLength];
        BitConverter.TryWriteBytes(request.AsSpan(0, sizeof(int)), (int)searchOption);
        BitConverter.TryWriteBytes(request.AsSpan(sizeof(int), sizeof(int)), pathByteCount);
        Encoding.UTF8.GetBytes(path, 0, path.Length, request, 2 * sizeof(int));
        Encoding.UTF8.GetBytes(searchPattern, 0, searchPattern.Length, request, requestLength - searchPatternByteCount);

        byte[] response = await SendRequestAsync(ActionType.GetFiles, request, cancellationToken).ConfigureAwait(false);
        List<string> files = new();
        for (int i = 0; i < response.Length;)
        {
            int fileByteCount = BitConverter.ToInt32(response.AsSpan(i, sizeof(int)));
            string file = Encoding.UTF8.GetString(response, i + sizeof(int), fileByteCount);
            files.Add(file);

            i += sizeof(int) + fileByteCount;
        }
        return files;
    }

    /// <inheritdoc/>
    public bool FileExists([NotNullWhen(true)] string? path)
    {
        using CancellationTokenSource? tokenSource = _timeout > 0 ? new(_timeout) : null;
        CancellationToken cancellationToken = tokenSource?.Token ?? default;
        return FileExistsAsync(path, cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(ActionType.FileExists, request, cancellationToken).ConfigureAwait(false);
        return BitConverter.ToBoolean(response);
    }

    /// <inheritdoc/>
    public DateTime GetLastWriteTimeUtc(string path)
    {
        using CancellationTokenSource? tokenSource = _timeout > 0 ? new(_timeout) : null;
        CancellationToken cancellationToken = tokenSource?.Token ?? default;
        return GetLastWriteTimeUtcAsync(path).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask<DateTime> GetLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            return DateTime.MinValue;

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(ActionType.GetLastWriteTimeUtc, request, cancellationToken).ConfigureAwait(false);
        return new DateTime(BitConverter.ToInt64(response));
    }

    /// <inheritdoc/>
    public Stream OpenRead(string path)
    {
        using CancellationTokenSource? tokenSource = _timeout > 0 ? new(_timeout) : null;
        CancellationToken cancellationToken = tokenSource?.Token ?? default;
        return OpenReadAsync(path).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(ActionType.OpenRead, request, cancellationToken).ConfigureAwait(false);
        return new MemoryStream(response);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _client.Dispose();
        _writeLock.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Stop();
        await _client.DisposeAsync().ConfigureAwait(false);
        _writeLock.Dispose();
    }

    /// <summary>
    /// Starts the loop responsible for processing remote file system events.
    /// </summary>
    private void Start()
    {
        if (_readLoopCancellationTokenSource is not null)
            return;

        _readLoopCancellationTokenSource = new();
        _ = Task.Run(() => RunAsync(_readLoopCancellationTokenSource.Token).ContinueWith(_ => Stop()));
    }

    /// <summary>
    /// Stops the loop responsible for processing remote file system events.
    /// </summary>
    private void Stop()
    {
        using CancellationTokenSource? tokenSource = _readLoopCancellationTokenSource;
        _readLoopCancellationTokenSource = null;
        tokenSource?.Cancel();

        CancelAllRequests();
    }

    /// <summary>
    /// Cancels all pending requests.
    /// </summary>
    private void CancelAllRequests()
    {
        foreach (TaskCompletionSource<byte[]> request in _requests.Values)
            request.TrySetCanceled();

        _requests.Clear();
    }

    /// <summary>
    /// Runs the asynchronous read loop to process file system events.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        SslStream stream = _client.GetStream();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                (int id, _, byte[] data) = await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false);
                if (_requests.TryRemove(id, out TaskCompletionSource<byte[]>? request))
                    request.TrySetResult(data);
            }
            catch (SystemException e) when (e is EndOfStreamException or OperationCanceledException)
            {
                // EOF and operation cancellation are valid ways to stop the loop.
                break;
            }
            catch (RemoteFileSystemException e) when (e.InnerException is not null)
            {
                if (_requests.TryRemove(e.Id, out TaskCompletionSource<byte[]>? request))
                    request.TrySetException(e.InnerException);
            }
            catch (Exception e)
            {
                LoggingHelper.LogError(this, "Failed to process a packet: {Exception}", e);
                continue;
            }
        }

        CancelAllRequests();
    }

    /// <summary>
    /// Sends a request to the remote file system and awaits the response.
    /// </summary>
    /// <param name="action">The action type.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The response data.</returns>
    private async Task<byte[]> SendRequestAsync(ActionType action, byte[] data, CancellationToken cancellationToken)
    {
        if (_readLoopCancellationTokenSource is null || !_client.Connected)
            throw new InvalidOperationException("Cannot send a request without first establishing a connection to the server.");

        TaskCompletionSource<byte[]> taskCompletionSource = new();
        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cancellationTokenSource.Token;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ++_currentId;
            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());
            _requests[_currentId] = taskCompletionSource;

            SslStream stream = _client.GetStream();
            await WritePacketAsync(stream, _currentId, action, data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
        return await taskCompletionSource.Task.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public string GetFullPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (GetRootLength(path) == 0)
            path = Combine(_fileSystemState.CurrentDirectory, path);

        string result = RemoveRelativeSegments(path);
        return result.Length == 0 ? $"{_fileSystemState.DirectorySeparatorChar}" : result;
    }

    /// <inheritdoc/>
    public string? GetDirectoryName(string? path)
    {
        if (path is null or { Length: 0 })
            return null;

        int rootLength = GetRootLength(path);
        int end = path.Length;
        if (end <= rootLength)
            return string.Empty;

        char directorySeparatorChar = _fileSystemState.DirectorySeparatorChar;
        char altDirectorySeparatorChar = _fileSystemState.AltDirectorySeparatorChar;
        while (end > rootLength)
        {
            char currentChar = path[--end];
            if (currentChar == directorySeparatorChar || currentChar == altDirectorySeparatorChar)
                break;
        }

        while (end > rootLength)
        {
            char currentChar = path[end - 1];
            if (currentChar == directorySeparatorChar || currentChar == altDirectorySeparatorChar)
            {
                end--;
            }
            else
            {
                break;
            }
        }

        return path.Substring(0, end);
    }

    /// <inheritdoc/>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetFileName(string? path)
    {
        if (path is null)
            return null;

        int root = GetRootLength(path);
        char directorySeparatorChar = _fileSystemState.DirectorySeparatorChar;
        char altDirectorySeparatorChar = _fileSystemState.AltDirectorySeparatorChar;
        int i = directorySeparatorChar == altDirectorySeparatorChar ?
            path.LastIndexOf(directorySeparatorChar) :
            path.LastIndexOfAny([directorySeparatorChar, altDirectorySeparatorChar]);

        return path.Substring(i < root ? root : i + 1);
    }

    /// <inheritdoc/>
    public string ChangeExtension(string path, string? extension)
#pragma warning disable RS0030 // Do not use banned APIs
        => Path.ChangeExtension(path, extension);
#pragma warning restore RS0030 // Do not use banned APIs

    /// <inheritdoc/>
    public string Combine(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1))
            return path2;

        if (string.IsNullOrEmpty(path2))
            return path1;

        if (GetRootLength(path2) != 0)
            return path2;

        char dir = _fileSystemState.DirectorySeparatorChar;
        char altDir = _fileSystemState.AltDirectorySeparatorChar;
        char l = path1[path1.Length - 1];
        char r = path2[0];
        bool hasSeparator = l == dir || l == altDir || r == dir || r == altDir;
        return hasSeparator ? $"{path1}{path2}" : $"{path1}{dir}{path2}";
    }

    /// <summary>
    /// Gets the length of the root part of the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>The length of the root part of the path.</returns>
    private int GetRootLength(string? path)
    {
        if (path is null or { Length: 0 })
            return 0;

        char dir = _fileSystemState.DirectorySeparatorChar;
        char altDir = _fileSystemState.AltDirectorySeparatorChar;
        if (path[0] == dir || path[0] == altDir)
            return 1;

        char vol = _fileSystemState.VolumeSeparatorChar;
        if (dir != altDir && path.Length > 1 && path[1] == vol && char.IsLetter(path[0]))
            return path.Length > 2 && (path[2] == dir || path[2] == altDir) ? 3 : 2;

        return 0;
    }

    /// <summary>
    /// Removes relative path segments from the specified path.
    /// </summary>
    /// <param name="path">The path to clean up.</param>
    /// <returns>The path with relative segments removed.</returns>
    private string RemoveRelativeSegments(string path)
    {
        StringBuilder builder = new(path.Length);
        bool flippedSeparator = false;

        char dir = _fileSystemState.DirectorySeparatorChar;
        char altDir = _fileSystemState.AltDirectorySeparatorChar;

        int rootLength = GetRootLength(path);
        int skip = rootLength;
        if (path[skip - 1] == dir || path[skip - 1] == altDir)
            skip--;

        if (skip > 0)
            builder.Append(path, 0, skip);

        for (int i = skip; i < path.Length; i++)
        {
            char c = path[i];

            if ((c == dir || c == altDir) && i + 1 < path.Length)
            {
                if (path[i + 1] == dir || path[i + 1] == altDir)
                    continue;

                if ((i + 2 == path.Length || path[i + 2] == dir || path[i + 2] == altDir) && path[i + 1] == '.')
                {
                    i++;
                    continue;
                }

                if (i + 2 < path.Length && (i + 3 == path.Length || path[i + 3] == dir || path[i + 3] == altDir) && path[i + 1] == '.' && path[i + 2] == '.')
                {
                    int s;
                    for (s = builder.Length - 1; s >= skip; s--)
                    {
                        if (builder[s] == dir || builder[s] == altDir)
                        {
                            builder.Length = (i + 3 >= path.Length && s == skip) ? s + 1 : s;
                            break;
                        }
                    }

                    if (s < skip)
                        builder.Length = skip;

                    i += 2;
                    continue;
                }
            }

            if (c != dir && c == altDir)
            {
                c = dir;
                flippedSeparator = true;
            }

            builder.Append(c);
        }

        if (!flippedSeparator && builder.Length == path.Length)
            return path;

        if (skip != rootLength && builder.Length < rootLength)
            builder.Append(path[rootLength - 1]);

        return builder.ToString();
    }
}
