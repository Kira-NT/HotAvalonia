using System.Net.Security;
using System.Text;
using HotAvalonia.Helpers;
using HotAvalonia.Net;

namespace HotAvalonia.IO;

/// <summary>
/// Provides functionality for monitoring file system changes remotely.
/// </summary>
internal sealed partial class RemoteFileSystemWatcher : IFileSystemWatcher
{
    /// <summary>
    /// The file system associated with this instance.
    /// </summary>
    private readonly RemoteFileSystem _fileSystem;

    /// <summary>
    /// The TCP client used for communication with the remote file system.
    /// </summary>
    private readonly SslTcpClient _client;

    /// <summary>
    /// The cancellation token source used to stop the read loop.
    /// </summary>
    private CancellationTokenSource? _readLoopCancellationTokenSource;

    /// <summary>
    /// The path of the directory to watch.
    /// </summary>
    private string _path;

    /// <summary>
    /// A value indicating whether the component is enabled.
    /// </summary>
    private bool _enableRaisingEvents;

    /// <summary>
    /// A value indicating whether subdirectories within the specified path should be monitored.
    /// </summary>
    private bool _includeSubdirectories;

    /// <summary>
    /// The filter string used to determine what files are monitored in a directory.
    /// </summary>
    private string _filter;

    /// <summary>
    /// The type of changes to watch for.
    /// </summary>
    private NotifyFilters _notifyFilters;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this instance.</param>
    /// <param name="client">The TCP client used for communication with the remote file system.</param>
    private RemoteFileSystemWatcher(RemoteFileSystem fileSystem, SslTcpClient client)
    {
        _fileSystem = fileSystem;
        _client = client;
        _readLoopCancellationTokenSource = null;

        _path = string.Empty;
        _enableRaisingEvents = false;
        _includeSubdirectories = false;
        _filter = "*.*";
        _notifyFilters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RemoteFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this instance.</param>
    /// <param name="client">The TCP client used for communication with the remote file system</param>
    /// <returns>A new instance of the <see cref="RemoteFileSystemWatcher"/> class.</returns>
    public static RemoteFileSystemWatcher Create(RemoteFileSystem fileSystem, SslTcpClient client)
    {
        _ = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _ = client ?? throw new ArgumentNullException(nameof(client));

        RemoteFileSystemWatcher watcher = new(fileSystem, client);
        watcher.Start();
        return watcher;
    }

    /// <inheritdoc/>
    public IFileSystem FileSystem => _fileSystem;

    /// <inheritdoc/>
    public string Path
    {
        get => _path;
        set => SetValue
        (
            ref _path,
            value ?? throw new ArgumentNullException(nameof(value)),
            ActionType.SetPath,
            Encoding.UTF8.GetBytes(value)
        );
    }

    /// <inheritdoc/>
    public bool EnableRaisingEvents
    {
        get => _enableRaisingEvents;
        set => SetValue
        (
            ref _enableRaisingEvents,
            value,
            ActionType.SetEnableRaisingEvents,
            BitConverter.GetBytes(value)
        );
    }

    /// <inheritdoc/>
    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set => SetValue
        (
            ref _includeSubdirectories,
            value,
            ActionType.SetIncludeSubdirectories,
            BitConverter.GetBytes(value)
        );
    }

    /// <inheritdoc/>
    public string Filter
    {
        get => _filter;
        set => SetValue
        (
            ref _filter,
            value ?? throw new ArgumentNullException(nameof(value)),
            ActionType.SetFilter,
            Encoding.UTF8.GetBytes(value)
        );
    }

    /// <inheritdoc/>
    public NotifyFilters NotifyFilter
    {
        get => _notifyFilters;
        set => SetValue
        (
            ref _notifyFilters,
            value,
            ActionType.SetNotifyFilter,
            BitConverter.GetBytes((int)value)
        );
    }

    /// <inheritdoc/>
    public event FileSystemEventHandler? Created;

    /// <inheritdoc/>
    public event FileSystemEventHandler? Deleted;

    /// <inheritdoc/>
    public event FileSystemEventHandler? Changed;

    /// <inheritdoc/>
    public event RenamedEventHandler? Renamed;

    /// <inheritdoc/>
    public event ErrorEventHandler? Error;

    /// <summary>
    /// Sets a field to a new value and sends a packet to the remote file system if the value changes.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field to update.</param>
    /// <param name="value">The new value.</param>
    /// <param name="action">The action to be performed on the remote file system.</param>
    /// <param name="data">The data to send as part of the packet.</param>
    private void SetValue<T>(ref T field, T value, ActionType action, byte[] data)
    {
        T oldValue = field;
        field = value;
        if (EqualityComparer<T>.Default.Equals(oldValue, value))
            return;

        SslStream stream = _client.GetStream();
        if (!_client.Connected)
            return;

        int timeout = _fileSystem.Timeout;
        using CancellationTokenSource? cancellationTokenSource = timeout > 0 ? new(timeout) : null;
        CancellationToken cancellationToken = cancellationTokenSource?.Token ?? default;

        // While it may seem like we can just fire and forget the write operation,
        // we need to make sure that the next write is not triggered before this one is finished.
        // This can be done either via a semaphore, or, since there are no expectations for
        // thread-safety of this class, by simply blocking the current thread until the write
        // is done. Guess which option we are going with.
        WritePacketAsync(stream, action, data, cancellationToken).GetAwaiter().GetResult();
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
    }

    /// <summary>
    /// Runs the asynchronous read loop to process file system events.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SslStream stream = _client.GetStream();
            ActionType action;
            byte[] data;

            try
            {
                (action, data) = await ReadPacketAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            catch (SystemException e) when (e is EndOfStreamException or OperationCanceledException)
            {
                // EOF and operation cancellation are valid ways to stop the loop.
                break;
            }
            catch (Exception e)
            {
                Error?.Invoke(this, new(e));
                break;
            }

            try
            {
                ExecuteAction(action, data);
            }
            catch (Exception e)
            {
                LoggingHelper.Log(this, "Could not execute {Action}: {Exception}", action, e);
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
            case ActionType.RaiseCreated:
                Created?.Invoke(this, ToFileSystemEventArgs(data));
                break;

            case ActionType.RaiseDeleted:
                Deleted?.Invoke(this, ToFileSystemEventArgs(data));
                break;

            case ActionType.RaiseChanged:
                Changed?.Invoke(this, ToFileSystemEventArgs(data));
                break;

            case ActionType.RaiseRenamed:
                Renamed?.Invoke(this, (RenamedEventArgs)ToFileSystemEventArgs(data));
                break;

            case ActionType.RaiseError:
                Error?.Invoke(this, new(RemoteFileSystemException.ToException(data)));
                break;
        }
    }

    /// <summary>
    /// Converts serialized event data represented as a byte array back to a <see cref="FileSystemEventArgs"/> instance.
    /// </summary>
    /// <param name="data">The byte array representing the serialized event data.</param>
    /// <returns>A <see cref="FileSystemEventArgs"/> instance based on the byte array content.</returns>
    private FileSystemEventArgs ToFileSystemEventArgs(byte[] data)
    {
        int fullPathByteCount = BitConverter.ToInt32(data.AsSpan(sizeof(byte), sizeof(int)));
        int mainByteCount = sizeof(byte) + sizeof(int) + fullPathByteCount;
        int oldFullPathByteCount = data.Length - mainByteCount;

        WatcherChangeTypes changeType = (WatcherChangeTypes)data[0];
        string fullPath = Encoding.UTF8.GetString(data, sizeof(byte) + sizeof(int), fullPathByteCount);
        if (oldFullPathByteCount <= 0)
            return _fileSystem.CreateFileSystemEventArgs(changeType, fullPath);

        string oldFullPath = Encoding.UTF8.GetString(data, mainByteCount, oldFullPathByteCount);
        return _fileSystem.CreateFileSystemEventArgs(changeType, fullPath, oldFullPath);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _client.Dispose();
    }
}
