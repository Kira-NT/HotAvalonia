using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
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
    /// <param name="name">The name of this file system.</param>
    /// <param name="client">The TCP client used for communication with the remote file system.</param>
    /// <param name="clientFactory">The factory function used to create authenticated TCP clients.</param>
    /// <param name="fileSystemState">The initial state of the file system.</param>
    private RemoteFileSystem(string name, SslTcpClient client, Func<CancellationToken, Task<SslTcpClient>> clientFactory, FileSystemState fileSystemState)
    {
        Name = name;
        _client = client;
        _clientFactory = clientFactory;
        _fileSystemState = fileSystemState;
        _requests = new();
        _writeLock = new(1, 1);
        _currentId = 0;
        _readLoopCancellationTokenSource = null;
        _timeout = DefaultTimeout;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string CurrentDirectory
    {
        get => _fileSystemState.CurrentDirectory;
        set => throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string TempDirectory => throw new NotImplementedException();

    /// <inheritdoc/>
    public StringComparison PathComparison => _fileSystemState.PathComparison;

    /// <inheritdoc/>
    public StringComparer PathComparer => _fileSystemState.PathComparer;

    /// <inheritdoc/>
    public char DirectorySeparator => _fileSystemState.DirectorySeparatorChar;

    /// <inheritdoc/>
    public char AltDirectorySeparator => _fileSystemState.AltDirectorySeparatorChar;

    /// <inheritdoc/>
    public char VolumeSeparator => _fileSystemState.VolumeSeparatorChar;

    /// <inheritdoc/>
    public char PathSeparator => throw new NotImplementedException();

    /// <summary>
    /// Gets or sets the length of time, in milliseconds, before a synchronous operation times out.
    /// </summary>
    public int Timeout
    {
        get => _timeout;
        set => _timeout = value is > 0 and < MinTimeout ? MinTimeout : value;
    }

    /// <inheritdoc/>
    public char[] InvalidPathChars => throw new NotImplementedException();

    /// <inheritdoc/>
    public char[] InvalidFileNameChars => throw new NotImplementedException();

    /// <inheritdoc/>
    public string[] GetLogicalDrives() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<string[]> GetLogicalDrivesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

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

        RemoteFileSystem fileSystem = new($"@{endpoint}", client, CreateClientAsync, state);
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
        await client.ConnectAsync(endpoint.Address, endpoint.Port).WithCancellation(cancellationToken).ConfigureAwait(false);

        SslTcpClient sslClient = await SslTcpClient.AuthenticateAsClientAsync(client, Hostname, cancellationToken).ConfigureAwait(false);
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
        using CancellationTokenSource? cancellationTokenSource = _readLoopCancellationTokenSource;
        _readLoopCancellationTokenSource = null;
        cancellationTokenSource?.Cancel();
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

        ushort taskId = 0;
        TaskCompletionSource<byte[]> taskCompletionSource = new();

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            taskId = ++_currentId;
            _requests[taskId] = taskCompletionSource;
            await WritePacketAsync(_client.GetStream(), taskId, action, data, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _requests.TryRemove(taskId, out _);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }

        try
        {
            using CancellationTokenRegistration cancellationContext = cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());
            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        finally
        {
            _requests.TryRemove(taskId, out _);
        }
    }

    /// <inheritdoc/>
    public IFileSystemWatcher CreateFileSystemWatcher()
    {
        using CancellationTokenSource? cancellationTokenSource = _timeout is int timeout and > 0 ? new(timeout) : null;
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;
        return CreateFileSystemWatcherAsync(cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask<IFileSystemWatcher> CreateFileSystemWatcherAsync(CancellationToken cancellationToken = default)
    {
        SslTcpClient client = await _clientFactory(cancellationToken).ConfigureAwait(false);
        await WritePacketAsync(client.GetStream(), 0, ActionType.CreateFileSystemWatcher, [], cancellationToken).ConfigureAwait(false);
        return RemoteFileSystemWatcher.Create(this, client);
    }

    /// <inheritdoc/>
    public bool DirectoryExists([NotNullWhen(true)] string? path)
        => GetBoolean(ActionType.DirectoryExists, path);

    /// <inheritdoc/>
    public ValueTask<bool> DirectoryExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => GetBooleanAsync(ActionType.DirectoryExists, path, cancellationToken);

    /// <inheritdoc/>
    public bool FileExists([NotNullWhen(true)] string? path)
        => GetBoolean(ActionType.FileExists, path);

    /// <inheritdoc/>
    public ValueTask<bool> FileExistsAsync([NotNullWhen(true)] string? path, CancellationToken cancellationToken = default)
        => GetBooleanAsync(ActionType.FileExists, path, cancellationToken);

    /// <inheritdoc/>
    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        using CancellationTokenSource? cancellationTokenSource = _timeout is int timeout and > 0 ? new(timeout) : null;
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;
        return OpenAsync(path, mode, access, share, cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public Task<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken = default)
    {
        if ((mode, access, share & ~FileShare.Read) == (FileMode.Open, FileAccess.Read, FileShare.None))
            return OpenReadAsync(path, cancellationToken);

        throw new NotImplementedException();
    }

    /// <inheritdoc cref="FileSystemExtensions.OpenReadAsync(IFileSystem, string, CancellationToken)"/>
    private async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(ActionType.OpenRead, request, cancellationToken).ConfigureAwait(false);
        return new MemoryStream(response);
    }

    /// <inheritdoc/>
    public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask CopyFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask MoveFileAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void MoveDirectory(string sourceDirName, string destDirName)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask MoveDirectoryAsync(string sourceDirName, string destDirName, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void ReplaceFile(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask ReplaceFileAsync(string sourceFileName, string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors = false, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void DeleteFile(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void DeleteDirectory(string path, bool recursive = false)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileSystemFileInfo CreateFileSymbolicLink(string path, string pathToTarget)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo> CreateFileSymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectorySymbolicLink(string path, string pathToTarget)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectorySymbolicLinkAsync(string path, string pathToTarget, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileSystemFileInfo? ResolveFileLinkTarget(string linkPath, bool returnFinalTarget)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileSystemFileInfo?> ResolveFileLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileSystemDirectoryInfo? ResolveDirectoryLinkTarget(string linkPath, bool returnFinalTarget)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo?> ResolveDirectoryLinkTargetAsync(string linkPath, bool returnFinalTarget, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateDirectory(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public string GetTempFileName()
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<string> GetTempFileNameAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileSystemDirectoryInfo CreateTempSubdirectory(string? prefix = null)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileSystemDirectoryInfo> CreateTempSubdirectoryAsync(string? prefix = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateDirectoriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        => GetFileSystemEntries(ActionType.GetFiles, path, searchPattern, searchOption);

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => GetFileSystemEntriesAsync(ActionType.GetFiles, path, searchPattern, searchOption, cancellationToken).ToAsyncEnumerable();

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public IAsyncEnumerable<string> EnumerateFileSystemEntriesAsync(string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public FileAttributes GetFileAttributes(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<FileAttributes> GetFileAttributesAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void SetFileAttributes(string path, FileAttributes fileAttributes)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetFileAttributesAsync(string path, FileAttributes fileAttributes, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetFileCreationTimeUtc(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetDirectoryCreationTimeUtc(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryCreationTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void SetFileCreationTimeUtc(string path, DateTime creationTimeUtc)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetFileCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void SetDirectoryCreationTimeUtc(string path, DateTime creationTimeUtc)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetDirectoryCreationTimeUtcAsync(string path, DateTime creationTimeUtc, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetFileLastAccessTimeUtc(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetDirectoryLastAccessTimeUtc(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastAccessTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void SetFileLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetFileLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void SetDirectoryLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastAccessTimeUtcAsync(string path, DateTime lastAccessTimeUtc, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetDirectoryLastWriteTimeUtc(string path)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask<DateTime> GetDirectoryLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetFileLastWriteTimeUtc(string path)
        => GetTimestamp(ActionType.GetLastWriteTimeUtc, path);

    /// <inheritdoc/>
    public ValueTask<DateTime> GetFileLastWriteTimeUtcAsync(string path, CancellationToken cancellationToken = default)
        => GetTimestampAsync(ActionType.GetLastWriteTimeUtc, path, cancellationToken);

    /// <inheritdoc/>
    public void SetFileLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetFileLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public void SetDirectoryLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueTask SetDirectoryLastWriteTimeUtcAsync(string path, DateTime lastWriteTimeUtc, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

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

    /// <inheritdoc/>
    public override string ToString() => this.ToFormattedString();

    /// <summary>
    /// Retrieves a boolean value based on the specified action and path.
    /// </summary>
    /// <param name="action">The type of action to perform.</param>
    /// <param name="path">The target path for the action.</param>
    /// <returns>A boolean value resulting from the operation.</returns>
    private bool GetBoolean(ActionType action, [NotNullWhen(true)] string? path)
    {
        using CancellationTokenSource? cancellationTokenSource = _timeout is int timeout and > 0 ? new(timeout) : null;
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;
        return GetBooleanAsync(action, path, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously retrieves a boolean value based on the specified action and path.
    /// </summary>
    /// <param name="action">The type of action to perform.</param>
    /// <param name="path">The target path for the action.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A boolean value resulting from the operation.</returns>
    private async ValueTask<bool> GetBooleanAsync(ActionType action, [NotNullWhen(true)] string? path, CancellationToken cancellationToken)
    {
        if (path is not { Length: > 0 })
            return false;

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(action, request, cancellationToken).ConfigureAwait(false);
        return BitConverter.ToBoolean(response);
    }

    /// <summary>
    /// Retrieves a timestamp based on the specified action and path.
    /// </summary>
    /// <param name="action">The type of action to perform.</param>
    /// <param name="path">The target path for the action.</param>
    /// <returns>A <see cref="DateTime"/> representing the timestamp from the operation.</returns>
    private DateTime GetTimestamp(ActionType action, string path)
    {
        using CancellationTokenSource? cancellationTokenSource = _timeout is int timeout and > 0 ? new(timeout) : null;
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;
        return GetTimestampAsync(action, path, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously retrieves a timestamp based on the specified action and path.
    /// </summary>
    /// <param name="action">The type of action to perform.</param>
    /// <param name="path">The target path for the action.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="DateTime"/> representing the timestamp from the operation.</returns>
    private async ValueTask<DateTime> GetTimestampAsync(ActionType action, string path, CancellationToken cancellationToken)
    {
        if (path is not { Length: > 0 })
            return FileSystem.s_missingFileSystemEntryTimestampUtc;

        byte[] request = Encoding.UTF8.GetBytes(path);
        byte[] response = await SendRequestAsync(action, request, cancellationToken).ConfigureAwait(false);
        return new DateTime(BitConverter.ToInt64(response), DateTimeKind.Utc);
    }

    /// <summary>
    /// Retrieves an enumerable collection of file names and/or directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="action">The action type.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern and option.</returns>
    private IEnumerable<string> GetFileSystemEntries(ActionType action, string path, string searchPattern, SearchOption searchOption)
    {
        using CancellationTokenSource? cancellationTokenSource = _timeout is int timeout and > 0 ? new(timeout) : null;
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;
        return GetFileSystemEntriesAsync(action, path, searchPattern, searchOption, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously retrieves an enumerable collection of file names and/or directory names that match a search pattern in a specified path, and optionally searches subdirectories.
    /// </summary>
    /// <param name="action">The action type.</param>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against file-system entries in <paramref name="path"/>.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of file-system entries in the directory specified by <paramref name="path"/> and that match the specified search pattern and option.</returns>
    private async Task<IEnumerable<string>> GetFileSystemEntriesAsync(ActionType action, string path, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));
        _ = searchPattern ?? throw new ArgumentNullException(nameof(searchPattern));

        int pathByteCount = Encoding.UTF8.GetByteCount(path);
        int searchPatternByteCount = Encoding.UTF8.GetByteCount(searchPattern);
        int requestLength = pathByteCount + searchPatternByteCount + 2 * sizeof(int);
        byte[] request = new byte[requestLength];
        BitConverter.TryWriteBytes(request.AsSpan(0, sizeof(int)), (int)searchOption);
        BitConverter.TryWriteBytes(request.AsSpan(sizeof(int), sizeof(int)), pathByteCount);
        Encoding.UTF8.GetBytes(path, 0, path.Length, request, 2 * sizeof(int));
        Encoding.UTF8.GetBytes(searchPattern, 0, searchPattern.Length, request, requestLength - searchPatternByteCount);

        byte[] response = await SendRequestAsync(action, request, cancellationToken).ConfigureAwait(false);
        List<string> entries = new();
        for (int i = 0; i < response.Length;)
        {
            int entryByteCount = BitConverter.ToInt32(response.AsSpan(i, sizeof(int)));
            string entry = Encoding.UTF8.GetString(response, i + sizeof(int), entryByteCount);
            entries.Add(entry);

            i += sizeof(int) + entryByteCount;
        }
        return entries;
    }
}
