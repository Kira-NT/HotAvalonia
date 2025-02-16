namespace HotAvalonia.IO;

/// <summary>
/// Represents a file system watcher that does nothing and never raises events.
/// </summary>
internal sealed class EmptyFileSystemWatcher : IFileSystemWatcher
{
    /// <inheritdoc/>
    public IFileSystem FileSystem { get; }

    /// <inheritdoc/>
    public string Path { get; set; }

    /// <inheritdoc/>
    public bool EnableRaisingEvents { get; set; }

    /// <inheritdoc/>
    public bool IncludeSubdirectories { get; set; }

    /// <inheritdoc/>
    public string Filter { get; set; }

    /// <inheritdoc/>
    public NotifyFilters NotifyFilter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyFileSystemWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system associated with this watcher.</param>
    public EmptyFileSystemWatcher(EmptyFileSystem fileSystem)
    {
        FileSystem = fileSystem;
        Path = string.Empty;
        EnableRaisingEvents = false;
        IncludeSubdirectories = false;
        Filter = "*.*";
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
    }

    /// <inheritdoc/>
    public event FileSystemEventHandler Created { add { } remove { } }

    /// <inheritdoc/>
    public event FileSystemEventHandler Deleted { add { } remove { } }

    /// <inheritdoc/>
    public event FileSystemEventHandler Changed { add { } remove { } }

    /// <inheritdoc/>
    public event RenamedEventHandler Renamed { add { } remove { } }

    /// <inheritdoc/>
    public event ErrorEventHandler Error { add { } remove { } }

    /// <inheritdoc/>
    public void Dispose() { }
}
