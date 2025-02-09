using System.Diagnostics;
using System.Net.Security;
using System.Text;
using HotAvalonia.Net;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for monitoring file system changes remotely.
/// </summary>
internal sealed partial class RemoteFileSystemWatcher : IDisposable
{
    /// <summary>
    /// The remote file system instance associated with this watcher.
    /// </summary>
    private readonly RemoteFileSystem _fileSystem;

    /// <summary>
    /// The TCP client used to send event notifications to the remote file system client.
    /// </summary>
    private readonly SslTcpClient _client;

    /// <summary>
    /// The underlying <see cref="FileSystemWatcher"/> used to monitor file system changes.
    /// </summary>
    private readonly FileSystemWatcher _watcher;

    /// <summary>
    /// A semaphore used to ensure exclusive write access to the network stream.
    /// </summary>
    private readonly SemaphoreSlim _writeLock;

    /// <summary>
    /// The cancellation token source used to cancel the asynchronous read loop.
    /// </summary>
    private CancellationTokenSource? _readLoopCancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The remote file system associated with this watcher.</param>
    /// <param name="client">The TCP client used to send event notifications to the remote file system client.</param>
    private RemoteFileSystemWatcher(RemoteFileSystem fileSystem, SslTcpClient client)
    {
        _fileSystem = fileSystem;
        _client = client;
        _writeLock = new(1, 1);
        _readLoopCancellationTokenSource = null;

        _watcher = new();
        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RemoteFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The remote file system associated with this watcher.</param>
    /// <param name="client">The TCP client used to send event notifications to the remote file system client.</param>
    /// <returns>A new instance of <see cref="RemoteFileSystemWatcher"/> class.</returns>
    public static RemoteFileSystemWatcher Create(RemoteFileSystem fileSystem, SslTcpClient client)
    {
        RemoteFileSystemWatcher watcher = new(fileSystem, client);
        watcher.Start();
        return watcher;
    }

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Created"/> event and sends
    /// a corresponding notification to the remote client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private async void OnCreated(object sender, FileSystemEventArgs args)
        => await SendEventAsync(ActionType.RaiseCreated, args);

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Deleted"/> event and sends
    /// a corresponding notification to the remote client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private async void OnDeleted(object sender, FileSystemEventArgs args)
        => await SendEventAsync(ActionType.RaiseDeleted, args);

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Changed"/> event and sends
    /// a corresponding notification to the remote client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private async void OnChanged(object sender, FileSystemEventArgs args)
        => await SendEventAsync(ActionType.RaiseChanged, args);

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Renamed"/> event and sends
    /// a corresponding notification to the remote client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private async void OnRenamed(object sender, RenamedEventArgs args)
        => await SendEventAsync(ActionType.RaiseRenamed, args);

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Error"/> event and sends
    /// a corresponding notification to the remote client.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private async void OnError(object sender, ErrorEventArgs args)
        => await SendEventAsync(ActionType.RaiseError, args);

    /// <summary>
    /// Asynchronously sends a file system event to the remote client.
    /// </summary>
    /// <param name="action">The action type representing the event.</param>
    /// <param name="args">The file system event arguments.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SendEventAsync(ActionType action, FileSystemEventArgs args, CancellationToken cancellationToken = default)
    {
        WatcherChangeTypes changeType = args.ChangeType;
        string fullPath = args.FullPath;
        string oldFullPath = (args as RenamedEventArgs)?.OldFullPath ?? string.Empty;

        int fullPathByteCount = Encoding.UTF8.GetByteCount(fullPath);
        int oldFullPathByteCount = Encoding.UTF8.GetByteCount(oldFullPath);
        int byteCount = sizeof(byte) + sizeof(int) + fullPathByteCount + oldFullPathByteCount;
        byte[] buffer = new byte[byteCount];

        buffer[0] = (byte)changeType;
        BitConverter.TryWriteBytes(buffer.AsSpan(sizeof(byte), sizeof(int)), fullPathByteCount);
        Encoding.UTF8.GetBytes(fullPath, buffer.AsSpan(sizeof(byte) + sizeof(int)));
        Encoding.UTF8.GetBytes(oldFullPath, buffer.AsSpan(sizeof(byte) + sizeof(int) + fullPathByteCount));

        await SendEventAsync(action, buffer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously sends an error event to the remote client.
    /// </summary>
    /// <param name="action">The action type representing the error event.</param>
    /// <param name="args">The error event arguments.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SendEventAsync(ActionType action, ErrorEventArgs args, CancellationToken cancellationToken = default)
    {
        byte[] buffer = RemoteFileSystemException.GetBytes(args.GetException());
        await SendEventAsync(action, buffer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously sends an event to the remote client.
    /// </summary>
    /// <param name="action">The action type representing the event.</param>
    /// <param name="data">The serialized event data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SendEventAsync(ActionType action, byte[] data, CancellationToken cancellationToken)
    {
        try
        {
            await UnsafeSendEventAsync(action, data, cancellationToken);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    /// <summary>
    /// Asynchronously sends an event with the specified to the remote client.
    /// </summary>
    /// <remarks>
    /// This method does not perform any exception handling whatsoever.
    /// </remarks>
    /// <param name="action">The action type representing the event.</param>
    /// <param name="data">The serialized event data.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task UnsafeSendEventAsync(ActionType action, byte[] data, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!_client.Connected)
                return;

            SslStream stream = _client.GetStream();
            await WritePacketAsync(stream, action, data, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Starts the asynchronous read loop.
    /// </summary>
    private void Start()
    {
        if (_readLoopCancellationTokenSource is not null)
            return;

        _readLoopCancellationTokenSource = new();
        _ = Task.Run(() => RunAsync(_readLoopCancellationTokenSource.Token).ContinueWith(_ => Dispose()));
    }

    /// <summary>
    /// Stops the asynchronous read loop.
    /// </summary>
    private void Stop()
    {
        using CancellationTokenSource? tokenSource = _readLoopCancellationTokenSource;
        _readLoopCancellationTokenSource = null;
        tokenSource?.Cancel();
    }

    /// <summary>
    /// Runs an asynchronous loop that continuously reads packets from
    /// the remote client and executes corresponding actions.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SslStream stream = _client.GetStream();
            (ActionType action, byte[] data) = await ReadPacketAsync(stream, cancellationToken);

            try
            {
                ExecuteAction(action, data);
            }
            catch
            {
                continue;
            }
        }
    }

    /// <summary>
    /// Executes the specified action using the provided data.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="data">The data associated with the action.</param>
    private void ExecuteAction(ActionType action, byte[] data)
    {
        switch (action)
        {
            case ActionType.SetPath:
                _watcher.Path = Encoding.UTF8.GetString(data);
                break;

            case ActionType.SetEnableRaisingEvents:
                _watcher.EnableRaisingEvents = BitConverter.ToBoolean(data);
                break;

            case ActionType.SetIncludeSubdirectories:
                _watcher.IncludeSubdirectories = BitConverter.ToBoolean(data);
                break;

            case ActionType.SetFilter:
                _watcher.Filter = Encoding.UTF8.GetString(data);
                break;

            case ActionType.SetNotifyFilter:
                _watcher.NotifyFilter = (NotifyFilters)BitConverter.ToInt32(data);
                break;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnCreated;
        _watcher.Deleted -= OnDeleted;
        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnError;
        _watcher.Dispose();
        _client.Dispose();
        _fileSystem.DetachRemoteFileSystemWatcher(this);
    }
}
