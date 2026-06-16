namespace HotAvalonia.IO;

/// <summary>
/// Listens to the local file system change notifications and raises
/// events when a directory, or file in a directory, changes.
/// </summary>
internal sealed class LocalFileSystemWatcher : FileSystemWatcher, IFileSystemWatcher
{
    /// <inheritdoc/>
    public IFileSystem FileSystem { get; }

    private FileChangeEventHandler? _created;
    private FileChangeEventHandler? _deleted;
    private FileChangeEventHandler? _changed;
    private FileRenameEventHandler? _renamed;
    private FileWatcherErrorEventHandler? _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this system.</param>
    public LocalFileSystemWatcher(LocalFileSystem fileSystem)
    {
        FileSystem = fileSystem;

        // Translate the BCL FileSystemWatcher events into HotAvalonia's platform-safe event types. This
        // class is only instantiated on desktop, where the base FileSystemWatcher is real; iOS uses
        // RemoteFileSystemWatcher and never constructs this type.
#pragma warning disable RS0030 // Do not use banned APIs — the base BCL FileSystemWatcher is the desktop source of events.
        base.Created += (_, e) => _created?.Invoke(this, new FileChangeEventArgs(e.ChangeType, e.FullPath));
        base.Deleted += (_, e) => _deleted?.Invoke(this, new FileChangeEventArgs(e.ChangeType, e.FullPath));
        base.Changed += (_, e) => _changed?.Invoke(this, new FileChangeEventArgs(e.ChangeType, e.FullPath));
        base.Renamed += (_, e) => _renamed?.Invoke(this, new FileRenameEventArgs(e.ChangeType, e.FullPath, e.OldFullPath));
        base.Error += (_, e) => _error?.Invoke(this, e.GetException());
#pragma warning restore RS0030
    }

    /// <inheritdoc/>
    event FileChangeEventHandler IFileSystemWatcher.Created { add => _created += value; remove => _created -= value; }

    /// <inheritdoc/>
    event FileChangeEventHandler IFileSystemWatcher.Deleted { add => _deleted += value; remove => _deleted -= value; }

    /// <inheritdoc/>
    event FileChangeEventHandler IFileSystemWatcher.Changed { add => _changed += value; remove => _changed -= value; }

    /// <inheritdoc/>
    event FileRenameEventHandler IFileSystemWatcher.Renamed { add => _renamed += value; remove => _renamed -= value; }

    /// <inheritdoc/>
    event FileWatcherErrorEventHandler IFileSystemWatcher.Error { add => _error += value; remove => _error -= value; }
}
